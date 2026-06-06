using BLL.DTO.Authority;
using Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BLL.AuthorityController
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Authority")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "authority")]
    public class AnalyticsController : ControllerBase
    {
        private readonly Ai_Reports_Context _context;

        public AnalyticsController(Ai_Reports_Context context)
        {
            _context = context;
        }

        [HttpGet("ResolutionRate")]
        public async Task<IActionResult> GetResolutionRate()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Unauthorized access.");

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category)) return NotFound("Authority category not found.");

            var total = await _context.TbReport
                .CountAsync(r => !r.IsDeleted &&
                                 r.Report_Category != null &&
                                 r.Report_Category.Contains(category)); // ✅

            if (total == 0) return Ok(new { Rate = "0%", Resolved = 0, Total = 0 });

            var resolved = await _context.TbReport
                .CountAsync(r => !r.IsDeleted &&
                                 r.Solved != null &&
                                 r.Report_Category != null &&
                                 r.Report_Category.Contains(category)); // ✅

            double rate = ((double)resolved / total) * 100;
            return Ok(new { Rate = rate.ToString("0.0") + "%", Resolved = resolved, Total = total });
        }

        [HttpGet("AverageResolutionTime")]
        public async Task<IActionResult> GetAverageResolutionTime()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Unauthorized access.");

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category)) return NotFound("Authority category not found.");

            var resolvedQuery = _context.TbReport
                .Where(r => r.Solved.HasValue &&
                            !r.IsDeleted &&
                            r.Report_Category != null &&
                            r.Report_Category.Contains(category)); // ✅

            if (!await resolvedQuery.AnyAsync())
                return Ok(new { AverageHours = 0, AverageFormatted = "0h 0m", TotalSolvedReports = 0 });

            var reportsWithDuration = await resolvedQuery
                .Select(r => new
                {
                    r.Report_ID,
                    DurationHours = (r.Solved!.Value - r.Report_Submit).TotalHours
                })
                .ToListAsync();

            double averageHours = reportsWithDuration.Average(x => x.DurationHours);
            var averageTimeSpan = TimeSpan.FromHours(averageHours);
            string formattedAverage = $"{(int)averageTimeSpan.TotalHours}h {averageTimeSpan.Minutes}m";

            return Ok(new
            {
                AverageHours = Math.Round(averageHours, 2),
                AverageFormatted = formattedAverage,
                TotalSolvedReports = reportsWithDuration.Count
            });
        }

        [HttpGet("TotalIncidents")]
        public async Task<IActionResult> GetTotalIncidents()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Unauthorized access.");

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category)) return NotFound("Authority category not found.");

            var count = await _context.TbReport
                .CountAsync(r => !r.IsDeleted &&
                                 r.Report_Category != null &&
                                 r.Report_Category.Contains(category)); // ✅

            return Ok(new { Total = count });
        }

        [HttpGet("GetAllReportsInAllMonths")]
        public async Task<IActionResult> GetMonthlyTrend()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Unauthorized access.");

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category)) return NotFound("Authority category not found.");

            var reportsData = await _context.TbReport
                .Where(r => !r.IsDeleted &&
                            r.Report_Category != null &&
                            r.Report_Category.Contains(category) && // ✅
                            r.CreatedAt.Year == DateTime.Now.Year)
                .GroupBy(r => r.CreatedAt.Month)
                .Select(g => new { MonthNumber = g.Key, Count = g.Count() })
                .ToListAsync();

            var finalResult = Enumerable.Range(1, 12).Select(m => new
            {
                Month = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m),
                Count = reportsData.FirstOrDefault(r => r.MonthNumber == m)?.Count ?? 0
            });

            return Ok(finalResult);
        }

        [HttpGet("GetAllReportsInAllMonthsOnlySolved")]
        public async Task<IActionResult> GetAllReportsInAllMonthsOnlyResolved()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Unauthorized access.");

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category)) return NotFound("Authority category not found.");

            var resolvedData = await _context.TbReport
                .Where(r => !r.IsDeleted &&
                            r.Report_Category != null &&
                            r.Report_Category.Contains(category) && // ✅
                            r.UpdatedStatus == 3 &&
                            r.CreatedAt.Year == DateTime.Now.Year)
                .GroupBy(r => r.CreatedAt.Month)
                .Select(g => new { MonthNumber = g.Key, Count = g.Count() })
                .ToListAsync();

            var finalResult = Enumerable.Range(1, 12).Select(m => new
            {
                Month = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m),
                Count = resolvedData.FirstOrDefault(r => r.MonthNumber == m)?.Count ?? 0
            });

            return Ok(finalResult);
        }

        private int GetCurrentAuthorityId()
        {
            var idClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        private async Task<string?> GetAuthorityCategory(int authId)
        {
            return await _context.TbAuthority
                .Where(a => a.Authority_ID == authId)
                .Select(a => a.Category)
                .FirstOrDefaultAsync();
        }
    }
}