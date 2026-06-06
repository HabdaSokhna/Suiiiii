using BLL.DTO.Authorization;
using BLL.Service;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BLL.Mangers.CitizenAccount
{
    public interface IForgetPassword
    {
        Task<(bool IsSuccess, string Message)> SendOtpAsync(ForgotPasswordDto model);
        Task<(bool IsSuccess, string Message)> ResetPasswordAsync(ResetPasswordDto model);
    }

    public class ForgetPassword : IForgetPassword
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly OtpStore _otpStore;
        private readonly ILogger<ForgetPassword> _logger;

        public ForgetPassword(
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            OtpStore otpStore,
            ILogger<ForgetPassword> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _otpStore = otpStore;
            _logger = logger;
        }

        // ✅ صفحة 1: بعت OTP
        public async Task<(bool IsSuccess, string Message)> SendOtpAsync(ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return (true, "لو الإيميل موجود هيوصلك كود");

            var otpCode = new Random().Next(100000, 999999).ToString();
            _otpStore.Save(model.Email, otpCode);
            await _emailService.SendOtpAsync(model.Email, otpCode);

            _logger.LogInformation("Reset OTP أُرسل للمستخدم {Email}", model.Email);
            return (true, "تم إرسال كود التحقق على الإيميل");
        }

        // ✅ صفحة 3: غير الباسورد باستخدام الـ Token المحفوظ
        public async Task<(bool IsSuccess, string Message)> ResetPasswordAsync(ResetPasswordDto model)
        {
            // جيب الـ Reset Token المحفوظ بعد التحقق من OTP
            var resetToken = _otpStore.GetToken($"reset_{model.Email}");
            if (resetToken == null)
                return (false, "انتهت الجلسة، ابعت OTP من أول");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return (false, "مستخدم مش موجود");

            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (!result.Succeeded)
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

            // امسح الـ Token بعد الاستخدام
            _otpStore.RemoveToken($"reset_{model.Email}");

            _logger.LogInformation("تم تغيير باسورد المستخدم {Email}", model.Email);
            return (true, "تم تغيير كلمة المرور بنجاح ✓");
        }
    }
}