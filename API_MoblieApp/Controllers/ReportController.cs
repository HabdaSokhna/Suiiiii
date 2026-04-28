using BLL.DTO.Report;
using BLL.Managers.ReportCitizen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Mime;

namespace BLL.Controllers
{
    /// <summary>
    /// Manages incident reports, including creation and historical retrieval for the authenticated citizen.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(GroupName = "citizen")]
    public class ReportsController : ControllerBase
    {
        private readonly ICreateReport _createReportManager;
        private readonly IGetHistoryManager _getHistoryManager;
        private readonly IGetReportById _getByIdManager;

        public ReportsController(
            ICreateReport createReportManager,
            IGetHistoryManager getHistoryManager,
            IGetReportById getByIdManager)
        {
            _createReportManager = createReportManager;
            _getHistoryManager = getHistoryManager;
            _getByIdManager = getByIdManager;
        }

        /// <summary>
        /// Creates a new incident report with optional media attachments.
        /// </summary>
        /// <param name="model">Report details submitted via form-data (supports file uploads).</param>
        /// <returns>The newly created report details.</returns>
        /// <response code="201">Report successfully created.</response>
        /// <response code="400">Invalid report data provided.</response>
        /// <response code="401">Unauthorized access.</response>
        [HttpPost("CreateReport")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateReport([FromForm] ReportCreate_Dto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid report data", errors = ModelState });

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

            var result = await _createReportManager.ExecuteAsync(model, userEmail);

            if (!result.IsSuccess) return BadRequest(result);

            return CreatedAtRoute("GetReportById", new { id = result.ReportId }, result);
        }

        /// <summary>
        /// Retrieves the list of incident reports submitted by the current user.
        /// </summary>
        /// <param name="filter">Optional query parameters for filtering and pagination.</param>
        /// <returns>A filtered list of user reports.</returns>
        /// <response code="200">Returns the report history.</response>
        /// <response code="401">Unauthorized access.</response>
        [HttpGet("History")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetReportHistory([FromQuery] ReportFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _getHistoryManager.ExecuteAsync(userId, filter, baseUrl);

            return Ok(result);
        }

        /// <summary>
        /// Gets detailed information for a specific report by its unique ID.
        /// </summary>
        /// <param name="id">The unique identifier of the report.</param>
        /// <returns>Detailed report object including status handles.</returns>
        /// <response code="200">Returns the requested report details.</response>
        /// <response code="404">Report not found or does not belong to the user.</response>
        [HttpGet("{id}", Name = "GetReportById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReportById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var report = await _getByIdManager.ExecuteAsync(id, userId, baseUrl);

            if (report == null) return NotFound(new { message = "Report not found or access denied." });

            return Ok(report);
        }
    }
}