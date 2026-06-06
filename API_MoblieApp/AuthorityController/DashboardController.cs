using BLL.DTO.Authority;
using Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BLL.AuthorityController
{
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

        private async Task<string?> GetAuthorityCategory(int authId)
        {
            return await _context.TbAuthority
                .Where(a => a.Authority_ID == authId)
                .Select(a => a.Category)
                .FirstOrDefaultAsync();
        }

        private int GetCurrentAuthorityId()
        {
            var idClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        [HttpGet("IncidentVolume")]
        public async Task<IActionResult> GetIncidentVolume()
        {
            try
            {
                int authId = GetCurrentAuthorityId();
                if (authId == 0) return Unauthorized();

                var category = await GetAuthorityCategory(authId);
                if (string.IsNullOrEmpty(category))
                    return BadRequest(new { message = "Authority category not found" });

                var lastWeek = DateTime.Now.AddDays(-7).Date;

                var queryData = await _context.TbReport
                    .Where(r => !string.IsNullOrEmpty(r.Report_Category) &&
                                r.Report_Category.Contains(category) && // ✅
                                r.Report_Submit.Date >= lastWeek &&
                                !r.IsDeleted)
                    .GroupBy(r => r.Report_Submit.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Pending = g.Count(r => r.UpdatedStatus == 1),
                        InProgress = g.Count(r => r.UpdatedStatus == 2),
                        Solved = g.Count(r => r.UpdatedStatus == 3),
                        Total = g.Count()
                    })
                    .ToListAsync();

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

        [HttpGet("TotalCount")]
        public async Task<IActionResult> GetTotalCount()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized();

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category))
                return BadRequest(new { message = "Authority category not found" });

            var count = await _context.TbReport
                .CountAsync(r => !r.IsDeleted &&
                                 r.Report_Category != null &&
                                 r.Report_Category.Contains(category)); // ✅

            return Ok(new { title = "Total Reports", value = count });
        }

        [HttpGet("PendingCount")]
        public async Task<IActionResult> GetPendingCount()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized();

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category))
                return BadRequest(new { message = "Authority category not found" });

            var count = await _context.TbReport
                .CountAsync(r => !r.IsDeleted &&
                                 r.Report_Category != null &&
                                 r.Report_Category.Contains(category) && // ✅
                                 r.UpdatedStatus == 1);

            return Ok(new { title = "Pending Reports", value = count });
        }

        [HttpGet("InProgressCount")]
        public async Task<IActionResult> GetInProgressCount()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized();

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category))
                return BadRequest(new { message = "Authority category not found" });

            var count = await _context.TbReport
                .CountAsync(r => !r.IsDeleted &&
                                 r.Report_Category != null &&
                                 r.Report_Category.Contains(category) && // ✅
                                 r.UpdatedStatus == 2);

            return Ok(new { title = "In Progress", value = count });
        }

        [HttpGet("SolvedCount")]
        public async Task<IActionResult> GetSolvedCount()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized();

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category))
                return BadRequest(new { message = "Authority category not found" });

            var count = await _context.TbReport
                .CountAsync(r => !r.IsDeleted &&
                                 r.Report_Category != null &&
                                 r.Report_Category.Contains(category) && 
                                 r.UpdatedStatus == 3);

            return Ok(new { title = "Solved Reports", value = count });
        }
        [HttpGet("HighPriorityCount")]
        [Authorize(Roles = "Authority")]
        public async Task<IActionResult> GetHighPriorityReports()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var authority = await _context.TbAuthority_Login
                .Include(a => a.Authority)
                .FirstOrDefaultAsync(a => a.Email == email);

            if (authority == null) return Unauthorized();

            var highPriorityCount = await _context.TbReport
                .AsNoTracking()
                .Where(r => !r.IsDeleted &&
                            r.Report_Category != null &&
                            r.Report_Category.Contains(authority.Authority.Category) &&
                            r.Confidence_Score * 100 > 85 ) // ✅ فوق 85 بس
                .CountAsync();

            return Ok(new
            {
                HighPriorityCount = highPriorityCount,
                Message = $"There are {highPriorityCount} high priority reports above 80% confidence"
            });
        }
        [HttpGet("LastFiveReports")]
        public async Task<IActionResult> GetLastFiveReports()
        {
            int authId = GetCurrentAuthorityId();
            if (authId == 0) return Unauthorized("Invalid Authority Token.");

            var category = await GetAuthorityCategory(authId);
            if (string.IsNullOrEmpty(category))
                return BadRequest(new { message = "Authority category not found" });

            var lastReports = await _context.TbReport
                .Where(r => !r.IsDeleted &&
                            r.Report_Category != null &&
                            r.Report_Category.Contains(category) && 
                            (r.LstHandle.Any(h => h.Authority_ID == authId) || r.UpdatedStatus == 1))
                .OrderByDescending(r => r.Report_ID)
                .Take(5)
                .Select(r => new
                {
                    r.Report_ID,
                    Category = r.Report_Category ?? "Unclassified",
                    Status = r.LstHandle
                                    .Where(h => h.Authority_ID == authId)
                                    .Select(h => h.Status)
                                    .FirstOrDefault() ?? "Pending",
                    SubmitDate = r.Report_Submit.ToString("yyyy-MM-dd HH:mm"),
                    Location = r.Report_GeoLocation,
                    CitizenName = r.Citizen.Citizen_Name ?? "Unknown"
                })
                .ToListAsync();

            return Ok(lastReports);
        }

        [HttpGet("RecentActions")]
        public async Task<IActionResult> GetRecentActions()
        {
            try
            {
                int authId = GetCurrentAuthorityId();
                if (authId == 0) return Unauthorized("Invalid Authority Token.");

                var category = await GetAuthorityCategory(authId);
                if (string.IsNullOrEmpty(category)) return NotFound("Authority category not found.");

                var result = await _context.TbHandle
                    .Include(h => h.Report)
                    .Where(h => h.Authority_ID == authId &&
                                !h.Report.IsDeleted &&
                                h.Report.Report_Category != null &&
                                h.Report.Report_Category.Contains(category)) // ✅
                    .OrderByDescending(h => h.Handle_ID)
                    .Take(3)
                    .Select(h => new SystemActionDto
                    {
                        ReportId = h.Report_ID,
                        Status = h.Status,
                        Time = h.LastUpdated,
                        Category = h.Report.Report_Category,
                        AI = h.Report.AI_Category
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error", error = ex.Message });
            }
        }
    }
}