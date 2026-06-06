using BLL.Managers.Notification;
using Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using System.Security.Claims;

namespace SIRS_API.AuthorityController
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(GroupName = "authority")]
    [Authorize(Roles = "Authority")]
    public class NotificationAuthorityController : ControllerBase
    {
        private readonly IAuthorityNotificationManager _notificationManager;
        private readonly Ai_Reports_Context _context;

        public NotificationAuthorityController(
            IAuthorityNotificationManager notificationManager,
            Ai_Reports_Context context)
        {
            _notificationManager = notificationManager;
            _context = context;
        }

        // ✅ جيب كل الإشعارات بتاعت الـ Authority
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var authIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authIdClaim) || !int.TryParse(authIdClaim, out int authorityId))
                return Unauthorized(new { message = "هوية الجهة غير موجودة في التوكن." });

            var notifications = await _notificationManager.GetMyHistoryAsync(authorityId);
            return Ok(notifications);
        }

        // ✅ عدد الإشعارات الغير مقروءة
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var authIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authIdClaim) || !int.TryParse(authIdClaim, out int authorityId))
                return Unauthorized(new { message = "هوية الجهة غير موجودة في التوكن." });

            var count = await _context.TbAuthorityNotification
                .CountAsync(n => n.Authority_ID == authorityId && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        // ✅ اتشال إشعار كـ مقروء
        [HttpPut("mark-as-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var result = await _notificationManager.MarkAsReadAsync(notificationId);

            if (!result)
                return NotFound(new { message = "الإشعار مش موجود." });

            return Ok(new { message = "تم تحديد الإشعار كمقروء ✓" });
        }

        // ✅ اتشال كل الإشعارات كـ مقروءة
        [HttpPut("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var authIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authIdClaim) || !int.TryParse(authIdClaim, out int authorityId))
                return Unauthorized(new { message = "هوية الجهة غير موجودة في التوكن." });

            var notifications = await _context.TbAuthorityNotification
                .Where(n => n.Authority_ID == authorityId && !n.IsRead)
                .ToListAsync();

            if (!notifications.Any())
                return Ok(new { message = "مفيش إشعارات غير مقروءة." });

            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"تم تحديد {notifications.Count} إشعار كمقروء ✓" });
        }
    }
}