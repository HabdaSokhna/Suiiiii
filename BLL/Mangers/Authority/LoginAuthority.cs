using BLL.DTO.Authority;
using BLL.DTO.Responce;
using BLL.Service; // عشان يشوف ITokenService
using Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Managers.Authority
{
    public interface ILoginAuthority
    {
        Task<AuthorityLoginResponseDto> LoginWithReportsAsync(AuthorityLoginDto model);
        Task<IEnumerable<AuthorityReportResponceDto>> GetReportsAfterLoginAsync(string email);
    }

    public class LoginAuthority : ILoginAuthority
    {
        private readonly Ai_Reports_Context _context;
        private readonly ITokenService _tokenService; // تعريف خدمة التوكن

        public LoginAuthority(Ai_Reports_Context context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<AuthorityLoginResponseDto> LoginWithReportsAsync(AuthorityLoginDto model)
        {
            var authAccount = await _context.TbAuthority_Login
                .FirstOrDefaultAsync(x => x.Email == model.Email && x.Password == model.Password);

            if (authAccount == null)
                return new AuthorityLoginResponseDto { IsSuccess = false, Message = "إيميل أو باسورد غلط" };

            // ✅ حفظ DeviceToken
            if (!string.IsNullOrEmpty(model.DeviceToken) && authAccount.DeviceToken != model.DeviceToken)
            {
                authAccount.DeviceToken = model.DeviceToken;
                await _context.SaveChangesAsync();
            }

            var token = _tokenService.GenerateToken(
                authAccount.Login_ID.ToString(),
                authAccount.Email,
                new List<string> { "Authority" }
            );

            return new AuthorityLoginResponseDto
            {
                IsSuccess = true,
                Message = "تم تسجيل الدخول بنجاح ✓",
                Token = token
            };
        }

        // 2. ميثود جلب التقارير
        public async Task<IEnumerable<AuthorityReportResponceDto>> GetReportsAfterLoginAsync(string email)
        {
            var authAccount = await _context.TbAuthority_Login
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email);

            if (authAccount == null)
            {
                return Enumerable.Empty<AuthorityReportResponceDto>();
            }

            var myReports = await _context.TbHandle
                .Include(h => h.Report)
                .Where(h => h.Authority_ID == authAccount.Authority_ID)
                .OrderByDescending(h => h.Report.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return myReports.Select(h => new AuthorityReportResponceDto
            {
                ReportId = h.Report_ID,
                Description = h.Report.Report_Description,
                Location = h.Report.Report_GeoLocation,
                PhotoPath = h.Report.PhotoPath,
                Status = h.Status,
                AICategory = h.Report.AI_Category,
                CreatedAt = h.Report.CreatedAt
            });
        }
    }
}