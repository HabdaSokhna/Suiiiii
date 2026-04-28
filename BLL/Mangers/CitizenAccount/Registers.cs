using BLL.DTO.Authorization;
using BLL.DTO.Responce;
using BLL.Managers.Notification;
using BLL.Managers.Notifications;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BLL.Mangers.CitizenAccount
{
    public interface IRegisters
    {
        Task<RegisterResponceDto> ExecuteAsync(RegisterDto model);
    }

    public class Registers : IRegisters
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Ai_Reports_Context _context;
        private readonly ICitizenNotificationManager _notificationManager;

        public Registers(
            UserManager<ApplicationUser> userManager,
            Ai_Reports_Context context,
            ICitizenNotificationManager notificationManager)
        {
            _userManager = userManager;
            _context = context;
            _notificationManager = notificationManager;
        }

        public async Task<RegisterResponceDto> ExecuteAsync(RegisterDto model)
        {
            // 1. التحقق المبدئي (زي ما هو)
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return new RegisterResponceDto { IsSuccess = false, Message = "البريد الإلكتروني مستخدم بالفعل." };

            // 2. استخدام الـ Strategy للتعامل مع الـ Transactions في لينكس
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // ابدأ الـ Transaction جوه الـ Strategy
                using var transaction = await _context.Database.BeginTransactionAsync();
                ApplicationUser? user = null;

                try
                {
                    user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return new RegisterResponceDto { IsSuccess = false, Message = "فشل Identity", Errors = result.Errors.Select(e => e.Description) };
                    }

                    await _userManager.AddToRoleAsync(user, "Citizen");

                    var citizenProfile = new Citizen
                    {
                        ApplicationUserId = user.Id,
                        Citizen_Name = model.FullName,
                        Citizen_National_Id = model.NationalId,
                        CreatedAt = DateTime.UtcNow
                    };
                    citizenProfile.LstPhone.Add(new Citizen_Phone { Phone_Number = model.PhoneNumber });

                    _context.TbCitizen.Add(citizenProfile);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _ = _notificationManager.FillAndSendAsync(citizenProfile.Citizen_ID, "Register");

                    return new RegisterResponceDto { IsSuccess = true, Message = "نجاح" };
                }
                catch (Exception ex)
                {
                    // الحماية: لا تنادي Rollback إلا لو الـ Connection لسه مفتوح
                    if (transaction.GetDbTransaction().Connection != null)
                    {
                        await transaction.RollbackAsync();
                    }

                    if (user != null) await _userManager.DeleteAsync(user);

                    return new RegisterResponceDto { IsSuccess = false, Message = "خطأ داتابيز", Errors = new List<string> { ex.Message } };
                }
            });
        }
    }
}