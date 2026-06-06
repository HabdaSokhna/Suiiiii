using BLL.Service;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BLL.Mangers.CitizenAccount
{
    public interface IOtp
    {
        Task<OtpResultDto> VerifyAsync(string email, string code, string? deviceToken, string purpose = "Login");
    }

    public class OtpManager : IOtp
    {
        private readonly OtpStore _otpStore;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly Ai_Reports_Context _context;

        public OtpManager(
            OtpStore otpStore,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            Ai_Reports_Context context)
        {
            _otpStore = otpStore;
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
        }

        public async Task<OtpResultDto> VerifyAsync(string email, string code, string? deviceToken, string purpose = "Login")
        {
            var user = await _userManager.FindByEmailAsync(email);

            // ❌ كود غلط أو منتهي
            if (!_otpStore.Verify(email, code))
            {
                // لو Register ومش مفعّل → امسح البيانات
                if (user != null && !user.EmailConfirmed)
                {
                    var citizen = await _context.TbCitizen
                        .Include(c => c.LstPhone)
                        .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

                    if (citizen != null)
                    {
                        _context.TbCitizen.Remove(citizen);
                        await _context.SaveChangesAsync();
                    }

                    await _userManager.DeleteAsync(user);
                }

                return new OtpResultDto { IsSuccess = false, Message = "كود غلط أو منتهي" };
            }

            if (user == null)
                return new OtpResultDto { IsSuccess = false, Message = "مستخدم مش موجود" };

            // ✅ ForgotPassword → احفظ Reset Token وارجع تأكيد
            if (purpose == "ForgotPassword")
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                _otpStore.SaveToken($"reset_{email}", resetToken);

                return new OtpResultDto
                {
                    IsSuccess = true,
                    Message = "تم التحقق، أدخل الباسورد الجديد"
                };
            }

            // ✅ Register → فعّل الحساب وارجع Token
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                var rolesReg = await _userManager.GetRolesAsync(user);
                var tokenReg = _tokenService.GenerateToken(user.Id, user.Email ?? "", rolesReg);
                var citizenReg = await _context.TbCitizen
                    .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

                return new OtpResultDto
                {
                    IsSuccess = true,
                    Message = "تم التحقق وتفعيل الحساب ✓",
                    Token = tokenReg,
                    Expires = DateTime.UtcNow.AddDays(1),
                    Role = rolesReg.FirstOrDefault() ?? "Citizen",
                    UserName = user.UserName,
                    CitizenId = citizenReg?.Citizen_ID ?? 0
                };
            }

            // ✅ Login → رجّع Token + حدّث DeviceToken
            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateToken(user.Id, user.Email ?? "", roles);
            var citizen2 = await _context.TbCitizen
                .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            if (!string.IsNullOrEmpty(deviceToken))
            {
                bool isUpdated = false;

                if (citizen2 != null && citizen2.DeviceToken != deviceToken)
                {
                    citizen2.DeviceToken = deviceToken;
                    _context.TbCitizen.Update(citizen2);
                    isUpdated = true;
                }

                if (user.DeviceToken != deviceToken)
                {
                    user.DeviceToken = deviceToken;
                    await _userManager.UpdateAsync(user);
                    isUpdated = true;
                }

                if (isUpdated)
                    await _context.SaveChangesAsync();
            }

            return new OtpResultDto
            {
                IsSuccess = true,
                Message = "تم التحقق وتسجيل الدخول بنجاح ✓",
                Token = token,
                Expires = DateTime.UtcNow.AddDays(1),
                Role = roles.FirstOrDefault() ?? "Citizen",
                UserName = user.UserName,
                CitizenId = citizen2?.Citizen_ID ?? 0
            };
        }
    }

    public class OtpResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public DateTime? Expires { get; set; }
        public string? Role { get; set; }
        public string? UserName { get; set; }
        public int CitizenId { get; set; }
    }
}