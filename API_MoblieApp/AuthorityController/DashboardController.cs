using BLL.DTO.Authority;
using Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BLL.AuthorityController
{
    /// <summary>
    /// Controller for the Authority Dashboard. 
    /// Provides filtered summaries, trends, and real-time monitoring based on the logged-in Authority's ID.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Authority")]
    [ApiExplorerSettings(GroupName = "authority")]
    public class DashboardController : ControllerBase
    {
        private readonly Ai_Reports_Context _context;

        public DashboardController(Ai_Reports_Context context)
        {
            _context = context;
        }



        /// <summary>
        /// Tracks the volume of incident reports for the authority over the last 7 days.
        /// </summary>
        /// <remarks>
        /// Used for line charts. Filters data by Authority_ID and submission date.
        /// </remarks>
        /// <returns>A list of dates and report counts.</returns>
        [HttpGet("IncidentVolume")]
        public async Task<IActionResult> GetIncidentVolume()
        {
            try
            {
                int authId = GetCurrentAuthorityId();
                if (authId == 0) return Unauthorized();

                var authCategory = await _context.TbAuthority
                    .Where(a => a.Authority_ID == authId)
                    .Select(a => a.Category)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(authCategory))
                    return BadRequest(new { message = "Authority category not found" });

                var lastWeek = DateTime.Now.AddDays(-7).Date;

                var queryData = await _context.TbReport
                    .Include(r => r.LstHandle)
                    .Where(r =>
                        !string.IsNullOrEmpty(r.Report_Category) &&
                        r.Report_Category.ToLower().Trim() == authCategory.ToLower().Trim() &&
                        r.Report_Submit.Date >= lastWeek &&
                        !r.IsDeleted)
                    .GroupBy(r => r.Report_Submit.Date)
                    .Select(g => new
                    {
                        Date = g.Key,

                        // مفيش Handle = Pending
                        Pending = g.Count(r => !r.LstHandle.Any()),

                        // عنده Handle ولسه Solved = null = InProgress
                        InProgress = g.Count(r =>
                            r.LstHandle.Any() && r.Solved == null),

                        // Solved != null = تم الحل
                        Solved = g.Count(r => r.Solved != null),

                        Total = g.Count()
                    })
                    .ToListAsync();

                // نكمّل الأيام اللي مفيهاش data بـ 0
                var result = new List<IncidentVolumeDto>();

                for (int i = 6; i >= 0; i--)
                {
                    var day = DateTime.Now.AddDays(-i).Date;
                    var found = queryData.FirstOrDefault(d => d.Date == day);

                    result.Add(new IncidentVolumeDto
                    {
                        Day = day.ToString("dd/MM"),
                        PendingCount = found?.Pending ?? 0,
                        InProgressCount = found?.InProgress ?? 0,
                        SolvedCount = found?.Solved ?? 0,
                        Total = found?.Total ?? 0
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        /// <summary>
        /// Retrieves the total number of reports relevant to the current authority.
        /// </summary>
        /// <remarks>
        /// This includes all new reports (Pending) and reports assigned to this specific authority.
        /// </remarks>
        /// <response code="200">Returns the total count object.</response>
        [HttpGet("TotalCount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTotalCount()
        {
            int authId = GetCurrentAuthorityId();
            var count = await _context.TbReport
                .CountAsync(r => !r.IsDeleted && (r.LstHandle.Any(h => h.Authority_ID == authId) || r.UpdatedStatus == 1));

            return Ok(new { title = "Total Reports", value = count });
        }

        /// <summary>
        /// Retrieves the number of reports that are still in 'Pending' status.
        /// </summary>
        /// <remarks>
        /// These are reports that have been submitted by citizens but not yet claimed or processed by any authority.
        /// </remarks>
        [HttpGet("PendingCount")]
        public async Task<IActionResult> GetPendingCount()
        {
            var count = await _context.TbReport
                .CountAsync(r => !r.IsDeleted && r.UpdatedStatus == 1);

            return Ok(new { title = "Pending Reports", value = count });
        }

        /// <summary>
        /// Retrieves the count of reports currently being handled by this authority.
        /// </summary>
        /// <remarks>
        /// Filtered by status 'Progress' in the TbHandle table for the authenticated authority.
        /// </remarks>
        [HttpGet("InProgressCount")]
        public async Task<IActionResult> GetInProgressCount()
        {
            int authId = GetCurrentAuthorityId();
            var count = await _context.TbHandle
                .CountAsync(h => h.Authority_ID == authId && h.Status == "Progress" && !h.Report.IsDeleted);

            return Ok(new { title = "In Progress", value = count });
        }

        /// <summary>
        /// Retrieves the number of reports successfully resolved by this authority.
        /// </summary>
        /// <remarks>
        /// Tracks completed tasks where the handling status is marked as 'Resolved'.
        /// </remarks>
        [HttpGet("SolvedCount")]
        public async Task<IActionResult> GetSolvedCount()
        {
            int authId = GetCurrentAuthorityId();
            var count = await _context.TbHandle
                .CountAsync(h => h.Authority_ID == authId && h.Status == "Resolved" && !h.Report.IsDeleted);

            return Ok(new { title = "Solved Reports", value = count });
        }


        /// <summary>
        /// Retrieves the 5 most recent reports assigned to the current authority.
        /// </summary>
        /// <response code="200">Returns a list of the latest 5 activities.</response>
        [HttpGet("LastFiveReports")]
        public async Task<IActionResult> GetLastFiveReports()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Invalid Authority Token.");

            // التعديل: البحث في جدول الـ Reports مباشرة 
            // بشرط أن يكون البلاغ موجه لهذه الجهة (لو عندك AuthorityID في جدول الـ Report) 
            // أو البحث عن البلاغات التي لها سجل في Handle أو لسه Pending وموجهة للجهة

            var lastReports = await _context.TbReport
                .Where(r => !r.IsDeleted && (r.LstHandle.Any(h => h.Authority_ID == authId) || r.UpdatedStatus == 1))
                .OrderByDescending(r => r.Report_ID) // الترتيب برقم الـ ID لضمان الأحدث دائماً
                .Take(5)
                .Select(r => new
                {
                    r.Report_ID,
                    Category = r.Report_Category ?? "Unclassified",
                    // بنجيب الحالة من جدول الـ Handle لو موجود، ولو مش موجود تبقى Pending
                    Status = r.LstHandle.Where(h => h.Authority_ID == authId)
                                       .Select(h => h.Status)
                                       .FirstOrDefault() ?? "Pending",
                    SubmitDate = r.Report_Submit.ToString("yyyy-MM-dd HH:mm"),
                    Location = r.Report_GeoLocation,
                    CitizenName = r.Citizen.Citizen_Name ?? "Unknown"
                })
                .ToListAsync();

            return Ok(lastReports);
        }
        /// <summary>
        /// Retrieves the last 3 critical actions performed in the system.
        /// </summary>
        /// <remarks>
        /// This endpoint monitors the system timeline, specifically tracking 
        /// when reports are submitted, assigned to technicians, or resolved.
        /// </remarks>
        /// <returns>A list of the 3 most recent system actions.</returns>
        /// <response code="200">Returns the list of recent actions.</response>
        /// <response code="400">If an internal error occurs while fetching data.</response>
        [HttpGet("RecentActions")]
        [Authorize(Roles = "Authority")]
        public async Task<IActionResult> GetRecentActions()
        {
            try
            {
                int authId = GetCurrentAuthorityId();
                if (authId == 0) return Unauthorized("Invalid Authority Token.");

                var authority = await _context.TbAuthority
                    .Where(a => a.Authority_ID == authId)
                    .Select(a => new { a.Category })
                    .FirstOrDefaultAsync();

                if (authority == null) return NotFound("Authority category not found.");

                // ✅ بس الـ Reports اللي معندهاش أي Handle خالص
                var pendingActions = await _context.TbReport
                    .Where(r => !r.IsDeleted
                             && r.Report_Category == authority.Category
                             && !_context.TbHandle.Any(h => h.Report_ID == r.Report_ID))
                    .Select(r => new SystemActionDto
                    {
                        ReportId = r.Report_ID,
                        Status = "Pending",
                        Time = r.CreatedAt,
                        Category = r.Report_Category,
                        AI = r.AI_Category
                    })
                    .ToListAsync();

                // كل record في TbHandle = action مستقل
                var handledActions = await _context.TbHandle
                    .Include(h => h.Report)
                    .Where(h => h.Authority_ID == authId && !h.Report.IsDeleted)
                    .Select(h => new SystemActionDto
                    {
                        ReportId = h.Report_ID,
                        Status = h.Status == "Progress" ? "In Progress" :
                                 h.Status == "Resolved" ? "Solved" : h.Status,
                        Time = h.LastUpdated,
                        Category = h.Report.Report_Category,
                        AI = h.Report.AI_Category
                    })
                    .ToListAsync();

                var allTimeline = pendingActions
                    .Concat(handledActions)
                    .OrderByDescending(a => a.Time)
                    .Take(4)
                    .ToList();

                return Ok(allTimeline);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error", error = ex.Message });
            }
        }
        /// <summary>
        /// Helper method to extract Authority ID from the JWT uid claim.
        /// </summary>
        private int GetCurrentAuthorityId()
        {
            var idClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}