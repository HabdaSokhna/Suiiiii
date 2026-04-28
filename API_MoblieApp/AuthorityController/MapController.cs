using BLL.DTO.Authority;
using BLL.Mangers.Authority;
using Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BLL.AuthorityController
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "authority")]
    [ApiController]
    [Authorize(Roles = "Authority")] // تأمين الـ API بحيث لا يدخله إلا جهة مخولة
    public class MapController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        

        public MapController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
            
        }

        /// <summary>
        /// Get reports with geolocation data filtered by the logged-in Authority (via Token).
        /// </summary>
        [HttpGet("GetReportsMapData")]
        public async Task<IActionResult> GetReportsMapData()
        {
           
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int authId))
            {
                return Unauthorized("Invalid or missing Authority Token.");
            }
           

            var result = await _analyticsService.GetReportsMapDataAsync(authId);

            if (result == null)
                return Ok(new List<MapReportDto>());

            return Ok(result);
        }
    }
}