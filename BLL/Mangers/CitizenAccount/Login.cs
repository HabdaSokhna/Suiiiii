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
        private readonly EmailService _emailService;  // ✅ جديد
        private readonly OtpStore _otpStore;          // ✅ جديد

        public LoginCitizenManager(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            Ai_Reports_Context context,
            ICitizenNotificationManager notificationManager,
            ILogger<LoginCitizenManager> logger,
            EmailService emailService,  // ✅ جديد
            OtpStore otpStore)          // ✅ جديد
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
            _notificationManager = notificationManager;
            _logger = logger;
            _emailService = emailService;
            _otpStore = otpStore;
        }

        public async Task<AuthorizationResponceDto> ExecuteAsync(LoginDto model)
        {
            ApplicationUser? user = model.EmailorPhoneNumber.Contains("@")
                ? await _userManager.FindByEmailAsync(model.EmailorPhoneNumber)
                : await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.EmailorPhoneNumber);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return new AuthorizationResponceDto { IsSuccess = false, Message = "بيانات الدخول غير صحيحة." };

            // ✅ دخول مباشر من غير أي check على DeviceToken
            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateToken(user.Id, user.Email ?? "", roles);
            var citizen = await _context.TbCitizen.FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            // ✅ حدّث الـ DeviceToken لو موجود في الـ Request
            if (!string.IsNullOrEmpty(model.DeviceToken))
            {
                bool isUpdated = false;

                if (citizen != null && citizen.DeviceToken != model.DeviceToken)
                {
                    citizen.DeviceToken = model.DeviceToken;
                    _context.TbCitizen.Update(citizen);
                    isUpdated = true;
                }

                if (user.DeviceToken != model.DeviceToken)
                {
                    user.DeviceToken = model.DeviceToken;
                    await _userManager.UpdateAsync(user);
                    isUpdated = true;
                }

                if (isUpdated)
                    await _context.SaveChangesAsync();
            }
            if (citizen != null)
            {
                try
                {
                    await _notificationManager.FillAndSendAsync(citizen.Citizen_ID, "Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Notification failed: {Message}", ex.Message);
                }
            }

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