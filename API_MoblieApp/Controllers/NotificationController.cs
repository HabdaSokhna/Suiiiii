using Database;
using Database.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRS_API.DTO.Notification;
using System.Security.Claims;

namespace SIRS_API.Controllers
{
    /// <summary>
    /// Manages user notifications, including retrieval and internal logging of system events.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly Ai_Reports_Context _context;

        public NotificationsController(Ai_Reports_Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all notifications for the authenticated citizen, ordered by most recent.
        /// </summary>
        /// <returns>A list of notification objects containing title, message, type, and timestamp.</returns>
        /// <response code="200">Returns the list of notifications.</response>
        /// <response code="404">If the citizen record is not found in the system.</response>
        [HttpGet("GetMyNotifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var citizen = await _context.TbCitizen.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (citizen == null) return NotFound(new { message = "Citizen not found" });

            var result = await _context.TbNotification
                .Where(n => n.Citizen_ID == citizen.Citizen_ID)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationResponse_Dto
                {
                    Id = n.Notification_ID.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt
                }).ToListAsync();

            return Ok(result);
        }
        [NonAction]
        private async Task FillNotificationTable(int citizenId, string type)
        {
            string title = "";
            string message = "";

            switch (type)
            {
                case "Login":
                    title = "تنبيه أمان";
                    message = "تم تسجيل دخول جديد إلى حسابك. إذا لم تكن أنت، يرجى مراجعة نشاط الحساب.";
                    break;
                case "CreateAccount":
                    title = "مرحباً بك";
                    message = "تم إنشاء حسابك بنجاح. نحن سعداء بانضمامك إلينا في نظام SIRS.";
                    break;
                case "ChangeEmail":
                    title = "تحديث الحساب";
                    message = "تم تغيير البريد الإلكتروني المرتبط بحسابك بنجاح.";
                    break;
                case "ChangePassword":
                    title = "أمان الحساب";
                    message = "تم تحديث كلمة المرور الخاصة بك بنجاح. يرجى عدم مشاركتها مع أي شخص.";
                    break;
                case "CreateReport":
                    title = "تأكيد استلام بلاغ";
                    message = "تم استلام بلاغك بنجاح وهو الآن قيد المراجعة من قبل الفريق المختص.";
                    break;
                case "UploadPhoto":
                    title = "الملف الشخصي";
                    message = "تم تحديث صورتك الشخصية بنجاح.";
                    break;
                default:
                    title = "إشعار من النظام";
                    message = "يوجد تحديث جديد بخصوص نشاط حسابك.";
                    break;
            }

            var notification = new Notification
            {
                Citizen_ID = citizenId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            _context.TbNotification.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}