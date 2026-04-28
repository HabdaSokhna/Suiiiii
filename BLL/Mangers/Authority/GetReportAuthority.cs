using BLL.DTO.Authority;
using BLL.DTO.Responce;
using BLL.Service;
using CURD;
using Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Managers.Authority
{
    public interface IGetReportAuthority
    {
        Task<AuthorityLoginResponseDto> LoginWithReportsAsync(AuthorityLoginDto model);
    }

    public class GetReportAuthority : IGetReportAuthority
    {
        private readonly Ai_Reports_Context _context;
        // 1. تعريف السيرفس هنا
        private readonly ITokenService _tokenService;

        // 2. حقن السيرفس في الـ Constructor
        public GetReportAuthority(Ai_Reports_Context context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<AuthorityLoginResponseDto> LoginWithReportsAsync(AuthorityLoginDto model)
        {
          
            var authAccount = await _context.TbAuthority_Login
                .Include(a => a.Authority)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == model.Email && x.Password == model.Password);

            if (authAccount == null) return null;

           
            var token = _tokenService.GenerateToken(
                authAccount.Login_ID.ToString(),
                authAccount.Email,
                new List<string> { "Authority" }
            );

            
            var reports = await _context.TbReport
                .AsNoTracking()
                .Where(r => r.Report_Category == authAccount.Authority.Category && !r.IsDeleted)
                .OrderByDescending(r => r.Report_Submit)
                .Select(r => new AuthorityReportResponceDto
                {
                    ReportId = r.Report_ID,
                    Description = r.Report_Description,
                    Location = r.Report_GeoLocation,
                    PhotoPath = r.PhotoPath,
                    AICategory = r.AI_Category,
                    CreatedAt = r.Report_Submit
                })
                .ToListAsync();

            
            return new AuthorityLoginResponseDto
            {
                Token = token,
                InitialReports = reports
            };
        }
    }
}