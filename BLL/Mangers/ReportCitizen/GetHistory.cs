using BLL.DTO.Report;
using Database;
using Microsoft.EntityFrameworkCore;

namespace BLL.Managers.ReportCitizen
{
    public interface IGetHistoryManager
    {
        Task<PagedResult<ReportSummary_Dto>> ExecuteAsync(string userId, ReportFilterDto filter, string baseUrl);
    }

    public class GetHistoryManager : IGetHistoryManager
    {
        private readonly Ai_Reports_Context _context;

        public GetHistoryManager(Ai_Reports_Context context)
        {
            _context = context;
        }

        public async Task<PagedResult<ReportSummary_Dto>> ExecuteAsync(string userId, ReportFilterDto filter, string baseUrl)
        {
            
            var citizenId = await _context.TbCitizen
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => c.Citizen_ID)
                .FirstOrDefaultAsync();

            if (citizenId == 0) return new PagedResult<ReportSummary_Dto>();

            
            var query = _context.TbReport
                .Where(r => r.Citizen_ID == citizenId && !r.IsDeleted)
                .AsQueryable();

            
            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(r => r.Report_Category == filter.Category);

            if (!string.IsNullOrEmpty(filter.Status))
            {
                
                query = query.Where(r => r.LstHandle
                    .OrderByDescending(h => h.LastUpdated)
                    .Select(h => h.Status)
                    .FirstOrDefault() == filter.Status);
            }

            
            var totalItems = await query.CountAsync();

           
            var reports = await query
                .OrderByDescending(r => r.Report_Submit)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new ReportSummary_Dto
                {
                    Report_ID = r.Report_ID,
                    Title = r.Report_Description.Split('\n', StringSplitOptions.None)[0].Replace("Title: ", ""), // استخراج العنوان
                    DisplayCategory = r.Report_Category ?? "General",
                   
                    PhotoUrl = string.IsNullOrEmpty(r.PhotoPath) ? null : $"{baseUrl}{r.PhotoPath}",
                   
                    Status = r.LstHandle
                        .OrderByDescending(h => h.LastUpdated)
                        .Select(h => h.Status)
                        .FirstOrDefault() ?? "Pending",
                    SubmittedAt = r.Report_Submit
                })
                .ToListAsync();

            return new PagedResult<ReportSummary_Dto>
            {
                TotalCount = totalItems,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Data = reports
            };
        }
    }
}
