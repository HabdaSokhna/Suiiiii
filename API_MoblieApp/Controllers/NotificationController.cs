using BLL.Managers.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;

namespace BLL.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(GroupName = "citizen")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationManager _notificationManager;

        public NotificationsController(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }

        [HttpGet("GetMyNotifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var result = await _notificationManager.GetMyHistoryAsync(userId);

                return Ok(result); // ✅ دايماً list حتى لو فاضية
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching notifications", error = ex.Message });
            }
        }

        [HttpPatch("{id}/mark-as-read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var success = await _notificationManager.MarkAsReadAsync(id);
                if (!success) return BadRequest(new { message = "Could not update notification status." });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error marking notification as read", error = ex.Message });
            }
        }
    }
}