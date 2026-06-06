using BLL.DTO.Responce;
using BLL.DTO.User;
using BLL.Service;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BLL.Managers.User
{
    public interface IProfileManager
    {
        Task<bool> ChangeEmailAsync(string userId, ChangeEmail_Dto model);
        Task<bool> ChangePasswordAsync(string userId, ChangePassword_Dto model);
        Task<string?> UploadPhotoAsync(string userId, IFormFile file, string webRootPath);
        Task<ProfileResponse_Dto?> GetProfileAsync(string userId, string baseUrl);
        Task<UserStatus_Dto?> GetUserStatusAsync(string userId, string baseUrl);
    }

    public class ProfileManager : IProfileManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Ai_Reports_Context _context;
        private readonly ISystemNotificationService _systemNotificationService;

        public ProfileManager(
            UserManager<ApplicationUser> userManager,
            Ai_Reports_Context context,
            ISystemNotificationService systemNotificationService)
        {
            _userManager = userManager;
            _context = context;
            _systemNotificationService = systemNotificationService;
        }
        public async Task<bool> ChangeEmailAsync(string userId, ChangeEmail_Dto model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                return false;

            user.Email = model.NewEmail;
            user.UserName = model.NewEmail; 

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await NotifyCitizen(userId, "ChangeEmail");
            }
            return result.Succeeded;
        }

        
        public async Task<bool> ChangePasswordAsync(string userId, ChangePassword_Dto model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
               
                await _userManager.UpdateSecurityStampAsync(user);
                await NotifyCitizen(userId, "ChangePassword");
            }
            return result.Succeeded;
        }

        public async Task<string?> UploadPhotoAsync(string userId, IFormFile file, string webRootPath)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            // 1. حدد اسم الفولدر الجديد اللي عايزه يظهر في الـ URL
            string folderName = "UserPhotos"; // ده الـ Route الجديد بتاعك

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // 2. بناء المسار النسبي (اللي هيتخزن في الداتابيز)
            var relativePath = Path.Combine("Uploads", folderName, fileName).Replace("\\", "/");

            // 3. بناء المسار الكامل (اللي السيستم هيسيف فيه فعلياً)
            var fullPath = Path.Combine(webRootPath, relativePath);

            // تأكد إن الفولدر موجود، ولو مش موجود الكود هيكريته
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // التخزين في الداتابيز بالـ Route الجديد
            user.ProfilePhotoPath = "/" + relativePath;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await NotifyCitizen(userId, "UploadPhoto");
                return user.ProfilePhotoPath;
            }
            return null;
        }


        public async Task<ProfileResponse_Dto?> GetProfileAsync(string userId, string baseUrl)
        {
            var user = await _userManager.Users
                .Include(u => u.CitizenProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.CitizenProfile == null) return null;

            return new ProfileResponse_Dto
            {
                FullName = user.CitizenProfile.Citizen_Name,
                Email = user.Email ?? "",
                PhotoUrl = string.IsNullOrEmpty(user.ProfilePhotoPath)
                    ? $"{baseUrl}/Uploads/Profiles/default-avatar.png"
                    : $"{baseUrl}{user.ProfilePhotoPath}"
            };
        }

        public async Task<UserStatus_Dto?> GetUserStatusAsync(string userId, string baseUrl)
        {
            var user = await _userManager.Users
                .Include(u => u.CitizenProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.CitizenProfile == null) return null;

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            // ✅ كل الريبورتات بدون أي فلترة شهر
            var allReports = await _context.TbReport
                .AsNoTracking()
                .Where(r => !r.IsDeleted
                         && r.Citizen_ID == user.CitizenProfile.Citizen_ID)
                .Select(r => new
                {
                    CreatedAt = r.CreatedAt,
                    Status = r.LstHandle
                        .OrderByDescending(h => h.Handle_ID)
                        .Select(h => h.Status)
                        .FirstOrDefault() ?? "Pending"
                })
                .ToListAsync();

            // ✅ CountReportsInMonth بيتحسب من الـ list مش query جديدة
            var countReportsInMonth = allReports
                .Count(r => r.CreatedAt >= startOfMonth && r.CreatedAt < endOfMonth);

            return new UserStatus_Dto
            {
                FullName = user.CitizenProfile.Citizen_Name,
                PhotoUrl = string.IsNullOrEmpty(user.ProfilePhotoPath)
                    ? $"{baseUrl}/Uploads/Profiles/default-avatar.png"
                    : $"{baseUrl}{user.ProfilePhotoPath}",
                TotalReports = allReports.Count,             // ✅ كل الريبورتات
                CountReportsInMonth = countReportsInMonth,   // ✅ عدد شهر الحالي بس
                PendingCount = allReports.Count(r => r.Status == "Pending"),
                InProgressCount = allReports.Count(r => r.Status == "In Progress"),
                ResolvedCount = allReports.Count(r => r.Status == "Resolved")
            };
        }
        private async Task NotifyCitizen(string userId, string type)
        {
            var citizenId = await _context.TbCitizen
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => c.Citizen_ID)
                .FirstOrDefaultAsync();

            if (citizenId != 0)
            {
                
                await _systemNotificationService.SendNotificationAsync(citizenId, type);
            }
        }
    }
}