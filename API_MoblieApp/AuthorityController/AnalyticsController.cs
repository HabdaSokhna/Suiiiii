using Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BLL.AuthorityController
{
    /// <summary>
    /// Provides statistical insights and analytics for the Smart Reporting System (SIRS).
    /// </summary>
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

        /// <summary>
        /// Retrieves the total number of reports ever submitted.
        /// </summary>
        /// <remarks>
        /// This endpoint counts all records in the Reports table where 'IsDeleted' is false.
        /// </remarks>
        /// <returns>An object containing the total count of reports.</returns>
        /// <response code="200">Returns the total count successfully.</response>
        [HttpGet("TotalIncidents")]
        public async Task<IActionResult> GetTotalIncidents()
        {
            // 1. جلب ID الجهة من التوكن
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Unauthorized access.");

            // 2. جلب تخصص الجهة (Category)
            var authorityCategory = await _context.TbAuthority
                .Where(a => a.Authority_ID == authId)
                .Select(a => a.Category)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(authorityCategory))
                return NotFound("Authority category not found.");

            var count = await _context.TbReport
                .CountAsync(r => r.Report_Category == authorityCategory && !r.IsDeleted);

            return Ok(new { Total = count });
        }

        /// <summary>
        /// Calculates the percentage of reports that have been successfully resolved.
        /// </summary>
        /// <remarks>
        /// It checks the 'LstHandle' collection for each report to see if any status is marked as "Resolved".
        /// </remarks>
        /// <returns>A string representing the resolution percentage (e.g., "75.5%").</returns>
        /// <response code="200">Returns the resolution rate successfully.</response>
        [HttpGet("ResolutionRate")]
        public async Task<IActionResult> GetResolutionRate()
        {
            int authId = GetCurrentAuthorityId();

            var total = await _context.TbHandle
                .CountAsync(h => h.Authority_ID == authId && !h.Report.IsDeleted);

            if (total == 0) return Ok(new { Rate = "0%" });

            var resolved = await _context.TbHandle
                .CountAsync(h => h.Authority_ID == authId && h.Status == "Resolved" && !h.Report.IsDeleted);

            double rate = ((double)resolved / total) * 100;
            return Ok(new { Rate = rate.ToString("0.0") + "%" });
        }
        /// <summary>
        /// Calculates the average resolution time for all solved reports in hours.
        /// </summary>
        // <summary>
        /// Calculates the average resolution time in hours and provides details for each report.
        /// </summary>
        /// <remarks>
        /// This endpoint returns the overall average resolution time for the authority 
        /// and a detailed list of all resolved reports with their individual resolution duration in hours.
        /// </remarks>
        /// <response code="200">Returns the average time and a list of report durations.</response>
        [HttpGet("AverageResolutionTime")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAverageResolutionTime()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized();

            // 1. Fetch only resolved reports for this authority
            var resolvedQuery = _context.TbReport
                .Where(r => r.Solved.HasValue
                         && !r.IsDeleted
                         && r.LstHandle.Any(h => h.Authority_ID == authId));

            if (!await resolvedQuery.AnyAsync())
                return Ok(new { averageHours = 0, message = "No resolved reports found.", reports = new List<object>() });

            // 2. Get individual report data with duration in hours
            // We calculate the difference in hours directly
            var reportsWithDuration = await resolvedQuery
                .Select(r => new
                {
                    r.Report_ID,
                    r.Report_Category,
                    SubmittedAt = r.Report_Submit,
                    ResolvedAt = r.Solved.Value,
                    // Calculation: (Resolved - Submit) converted to Total Hours
                    DurationHours = Math.Round((r.Solved.Value - r.Report_Submit).TotalHours, 2)
                })
                .ToListAsync();

            // 3. Calculate the overall average from the list
            double averageHours = reportsWithDuration.Average(x => x.DurationHours);

            // 4. Format the average for display (e.g., 5.5 hours -> 5h 30m)
            var averageTimeSpan = TimeSpan.FromHours(averageHours);
            string formattedAverage = $"{(int)averageTimeSpan.TotalHours}h {averageTimeSpan.Minutes}m";

            return Ok(new
            {
                AverageHours = Math.Round(averageHours, 2),
                AverageFormatted = formattedAverage,
                TotalSolvedReports = reportsWithDuration.Count
            });
        }

        /// <summary>
        /// Retrieves total report count aggregated by month for the current authority.
        /// </summary>
        [HttpGet("GetAllReportsInAllMonths")]
        public async Task<IActionResult> GetMonthlyTrend()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Unauthorized access.");

            var authorityCategory = await _context.TbAuthority
                .Where(a => a.Authority_ID == authId)
                .Select(a => a.Category)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(authorityCategory))
                return NotFound("Authority category not found.");

            var reportsData = await _context.TbReport
                .Where(r => !r.IsDeleted &&
                            r.Report_Category == authorityCategory &&
                            r.CreatedAt.Year == DateTime.Now.Year)
                .GroupBy(r => r.CreatedAt.Month)
                .Select(g => new
                {
                    MonthNumber = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var allMonths = Enumerable.Range(1, 12).Select(m => new
            {
                MonthNumber = m,
                MonthName = System.Globalization.CultureInfo.InvariantCulture
                                    .DateTimeFormat.GetMonthName(m)
            });

            var finalResult = allMonths.Select(m => new
            {
                Month = m.MonthName,
                Count = reportsData.FirstOrDefault(r => r.MonthNumber == m.MonthNumber)?.Count ?? 0
            });

            return Ok(finalResult);
        }

        /// <summary>
        /// Retrieves a monthly trend of resolved reports for the current authority.
        /// </summary>
        [HttpGet("GetAllReportsInAllMonthsOnlySolved")]
        public async Task<IActionResult> GetAllReportsInAllMonthsOnlyResolved()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized();

            var authorityCategory = await _context.TbAuthority
                .Where(a => a.Authority_ID == authId)
                .Select(a => a.Category)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(authorityCategory))
                return NotFound("Authority category not found.");

            // ✅ Solved = Report.Solved != null مش Handle.Status
            var resolvedData = await _context.TbReport
                .Where(r => !r.IsDeleted &&
                            r.Report_Category == authorityCategory &&
                            r.Solved != null &&
                            r.CreatedAt.Year == DateTime.Now.Year)
                .GroupBy(r => r.CreatedAt.Month)
                .Select(g => new
                {
                    MonthNumber = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var allMonths = Enumerable.Range(1, 12).Select(m => new
            {
                MonthNumber = m,
                MonthName = System.Globalization.CultureInfo.InvariantCulture
                                    .DateTimeFormat.GetMonthName(m)
            });

            var finalResult = allMonths.Select(m => new
            {
                Month = m.MonthName,
                Count = resolvedData.FirstOrDefault(r => r.MonthNumber == m.MonthNumber)?.Count ?? 0
            });

            return Ok(finalResult);
        }
        private int GetCurrentAuthorityId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}