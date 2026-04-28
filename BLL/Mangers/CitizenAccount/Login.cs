using BLL.DTO.Authorization;
using BLL.DTO.Responce;
using BLL.Managers.Notifications;
using BLL.Service;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Mangers.CitizenAccount
{
    public interface ILogin
    {
        Task<AuthorizationResponceDto> ExecuteAsync(LoginDto model);
    }

    public class LoginCitizenManager : ILogin
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly Ai_Reports_Context _context;
        private readonly ICitizenNotificationManager _notificationManager;
        private readonly ILogger<LoginCitizenManager> _logger;

        public LoginCitizenManager(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            Ai_Reports_Context context,
            ICitizenNotificationManager notificationManager,
            ILogger<LoginCitizenManager> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
            _notificationManager = notificationManager;
            _logger = logger;
        }

        public async Task<AuthorizationResponceDto> ExecuteAsync(LoginDto model)
        {
            // 1. التأكد من بيانات المستخدم (Email أو Phone)
            ApplicationUser? user = model.EmailorPhoneNumber.Contains("@")
                ? await _userManager.FindByEmailAsync(model.EmailorPhoneNumber)
                : await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.EmailorPhoneNumber);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return new AuthorizationResponceDto { IsSuccess = false, Message = "بيانات الدخول غير صحيحة." };
            }

            var roles = await _userManager.GetRolesAsync(user);

            // التعديل هنا: نبعت الـ Id والـ Email والـ Roles كـ نصوص (Strings)
            var token = _tokenService.GenerateToken(user.Id, user.Email ?? "", roles);

            // 3. جلب بيانات المواطن المرتبطة بهذا المستخدم
            var citizen = await _context.TbCitizen.FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            if (citizen != null)
            {
                // التحقق من وصول DeviceToken جديد من الـ Frontend
                if (!string.IsNullOrEmpty(model.DeviceToken))
                {
                    bool isUpdated = false;

                    // تحديث في جدول المواطن (TbCitizen)
                    if (citizen.DeviceToken != model.DeviceToken)
                    {
                        citizen.DeviceToken = model.DeviceToken;
                        _context.TbCitizen.Update(citizen);
                        isUpdated = true;
                    }

                    // تحديث في جدول المستخدمين (AspNetUsers) - السطر ده هو "الفيشة" اللي ناقصة
                    if (user.DeviceToken != model.DeviceToken)
                    {
                        user.DeviceToken = model.DeviceToken;
                        await _userManager.UpdateAsync(user); // تحديث Identity مباشرة
                        isUpdated = true;
                    }

                    if (isUpdated)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"✅ Device token synced for User: {user.Email}");
                    }
                }

                // 4. إرسال إشعار تسجيل الدخول (بعد التأكد من تحديث التوكن)
                try
                {
                    
                    await _notificationManager.FillAndSendAsync(citizen.Citizen_ID, "Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"⚠️ Notification failed but login continued: {ex.Message}");
                }
            }

            // 5. الرد النهائي بالـ Token والبيانات
            return new AuthorizationResponceDto
            {
                IsSuccess = true,
                Message = "تم تسجيل الدخول بنجاح.",
                Token = token,
                Expires = DateTime.UtcNow.AddDays(1),
                Role = roles.FirstOrDefault() ?? "Citizen",
                UserName = user.UserName,
                CitizenId = citizen?.Citizen_ID ?? 0
            };
        }
    }
}