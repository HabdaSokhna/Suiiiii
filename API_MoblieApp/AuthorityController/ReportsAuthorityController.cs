using BLL.DTO.Authority;
using BLL.Managers.Notifications;
using BLL.Service;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SIRS_API.AuthorityController
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "authority")]
    public class ReportsAuthorityController : ControllerBase
    {
        private readonly Ai_Reports_Context _context;
        private readonly INotificationService _notificationService;
        private readonly IGeocodingService _geocoding;
        private readonly ICitizenNotificationManager _notificationManager;

        public ReportsAuthorityController(
            Ai_Reports_Context context,
            INotificationService notificationService,
            IGeocodingService geocoding,
            ICitizenNotificationManager notificationManager)
        {
            _context = context;
            _notificationService = notificationService;
            _geocoding = geocoding;
            _notificationManager = notificationManager;
        }

        [HttpGet("GetAllReports")]
        [Authorize(Roles = "Authority")]
        public async Task<IActionResult> GetAllReports()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var authority = await _context.TbAuthority_Login
                .Include(a => a.Authority)
                .FirstOrDefaultAsync(a => a.Email == email);

            if (authority == null) return Unauthorized();

            var reports = await _context.TbReport
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.Report_Category == authority.Authority.Category)
                .OrderByDescending(r => r.Report_Submit)
                .Select(r => new
                {
                    r.Report_ID,
                    CitizenName = r.Citizen != null ? r.Citizen.Citizen_Name : "Unknown Citizen",
                    Category = r.Report_Category ?? "Unclassified",
                    Status = r.LstHandle
                        .OrderByDescending(h => h.LastUpdated)
                        .Select(h => h.Status)
                        .FirstOrDefault() ?? "Pending",
                    Location = r.Report_GeoLocation,   // ✅ جيب الـ raw coordinates الأول
                    DateTime = r.Report_Submit.ToString("dd/MM/yyyy HH:mm"),
                    Confidence = (r.Confidence_Score * 100).ToString("0.0") + "%"
                })
                .ToListAsync();

           
            var result = new List<object>();

            foreach (var r in reports)
            {
                var address = await _geocoding.GetAddressAsync(r.Location);
                await Task.Delay(1000); // Nominatim rate limit

                result.Add(new
                {
                    r.Report_ID,
                    r.CitizenName,
                    r.Category,
                    r.Status,
                    Location = address,          
                    RawLocation = r.Location,    
                    r.DateTime,
                    r.Confidence
                });
            }

            return Ok(result);
        }
        [HttpGet("GetReportDetails/{id}")]
        [Authorize(Roles = "Authority")]
        public async Task<IActionResult> GetReportDetails(int id)
        {
            var report = await _context.TbReport
                .Include(r => r.Citizen)
                .Include(r => r.LstHandle)
                    .ThenInclude(h => h.Authority)
                .FirstOrDefaultAsync(r => r.Report_ID == id && !r.IsDeleted);

            if (report == null)
                return NotFound(new { message = "البلاغ غير موجود أو تم حذفه." });

            // فصل العنوان عن الوصف
            string fullDescription = report.Report_Description ?? "";
            string title = "بدون عنوان";
            string descriptionBody = fullDescription;

            if (fullDescription.Contains("Title:") && fullDescription.Contains("Description:"))
            {
                var parts = fullDescription
                    .Replace("Title:", "")
                    .Split(new[] { "Description:" }, StringSplitOptions.None);

                if (parts.Length == 2)
                {
                    title = parts[0].Trim();
                    descriptionBody = parts[1].Trim();
                }
            }

            string status = report.Solved != null ? "Solved" :
                            report.LstHandle.Any() ? "In Progress" : "Pending";

            var address = await _geocoding.GetAddressAsync(report.Report_GeoLocation);

            return Ok(new
            {
                report.Report_ID,
                CitizenName = report.Citizen?.Citizen_Name ?? "مواطن غير مسجل",
                Report_Title = title,
                Report_Description = descriptionBody,
                Category = report.Report_Category,
                Status = status,
                Location = address,
                Time = report.CreatedAt.ToString("MMMM dd, yyyy, HH:mm"),
                Photo = report.PhotoPath,
                AI_Analysis = new
                {
                    Predicted = report.AI_Category,
                    Score = $"{(report.Confidence_Score * 100f):0.#}%"
                },
                History = report.LstHandle
                    .OrderByDescending(h => h.LastUpdated)
                    .Select(h => new
                    {
                        Status = h.Status == "Progress" ? "In Progress" :
                                 h.Status == "Resolved" ? "Solved" : h.Status,
                        UpdatedBy = h.Authority?.Authority_Name ?? "جهة غير محددة",
                        Time = h.LastUpdated.ToString("yyyy-MM-dd HH:mm")
                    })
            });
        }

   
        [HttpGet("GetReportActivity")]
        [Authorize(Roles = "Authority")]
        public async Task<IActionResult> GetReportActivity(int reportId)
        {
            var reportData = await _context.TbReport
                .Include(r => r.Citizen)
                .Include(r => r.LstHandle)
                .FirstOrDefaultAsync(r => r.Report_ID == reportId && !r.IsDeleted);

            if (reportData == null)
                return NotFound(new { message = "Report not found." });

            string citizenName = reportData.Citizen?.Citizen_Name ?? "مواطن غير مسجل";
            var timeline = new List<object>();

            // Pending — دايماً موجود
            timeline.Add(new
            {
                StatusName = "Pending",
                CitizenName = citizenName,
                Time = reportData.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                Message = GetActivityMessage("Pending")
            });

            // In Progress — لو فيه Handle
            var lastHandle = reportData.LstHandle
                .OrderByDescending(h => h.LastUpdated)
                .FirstOrDefault();

            if (lastHandle != null)
            {
                timeline.Add(new
                {
                    StatusName = "In Progress",
                    CitizenName = citizenName,
                    Time = lastHandle.LastUpdated.ToString("yyyy-MM-dd HH:mm"),
                    Message = GetActivityMessage("In Progress")
                });
            }

            // Resolved — لو الـ Solved متسجل
            if (reportData.Solved.HasValue)
            {
                timeline.Add(new
                {
                    StatusName = "Resolved",
                    CitizenName = citizenName,
                    Time = reportData.Solved.Value.ToString("yyyy-MM-dd HH:mm"),
                    Message = GetActivityMessage("Resolved")
                });
            }

            string currentStatus = reportData.Solved.HasValue ? "Solved" :
                                   lastHandle != null ? "In Progress" : "Pending";

            return Ok(new
            {
                Report_ID = reportId,
                CurrentStatus = currentStatus,
                Timeline = timeline.AsEnumerable().Reverse()
            });
        }
        [HttpPost("UpdateStatus")]
        [Authorize(Roles = "Authority")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusDto model)
        {
            var authIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authIdFromToken) || !int.TryParse(authIdFromToken, out int currentAuthorityId))
                return Unauthorized(new { message = "هوية الجهة غير موجودة في التوكن." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var report = await _context.TbReport
                    .FirstOrDefaultAsync(r => r.Report_ID == model.ReportId && !r.IsDeleted);

                if (report == null)
                    return NotFound(new { message = "البلاغ غير موجود." });

                report.UpdatedStatus = model.NewStatus;
                string statusText = GetStatusString(model.NewStatus);

                if (model.NewStatus == 3)
                    report.Solved = DateTime.UtcNow;

                _context.TbHandle.Add(new Handle
                {
                    Report_ID = model.ReportId,
                    Authority_ID = currentAuthorityId,
                    Status = statusText,
                    LastUpdated = DateTime.UtcNow.AddHours(2)
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // إرسال الإشعار خارج الـ Transaction
                try
                {
                    await _notificationManager.FillAndSendAsync(report.Citizen_ID, "ReportUpdate", statusText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ فشل إرسال الإشعار: {ex.Message}");
                }

                return Ok(new
                {
                    success = true,
                    message = $"تم تحديث البلاغ رقم {model.ReportId} إلى حالة {statusText}",
                    currentStatus = model.NewStatus
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "حدث خطأ داخلي أثناء التحديث.", details = ex.Message });
            }
        }
        [NonAction]
        private async Task SendStatusNotification(int citizenId, string status, int reportId)
        {
            try
            {
                string title = "تحديث بخصوص بلاغك";
                string message = status switch
                {
                    "In Progress" => $"تم استلام بلاغك رقم {reportId} وجاري العمل عليه الآن.",
                    "Resolved" => $"تم حل بلاغك رقم {reportId} بنجاح. شكراً لتعاونك!",
                    "Rejected" => $"نعتذر منك، تم رفض البلاغ رقم {reportId}.",
                    _ => $"تغيرت حالة بلاغك رقم {reportId} إلى {status}"
                };

                _context.TbNotification.Add(new Notification
                {
                    Citizen_ID = citizenId,
                    Title = title,
                    Message = message,
                    Type = "ReportUpdate",
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                var citizen = await _context.TbCitizen
                    .Where(c => c.Citizen_ID == citizenId)
                    .Select(c => new { c.DeviceToken })
                    .FirstOrDefaultAsync();

                if (citizen != null && !string.IsNullOrEmpty(citizen.DeviceToken))
                    await _notificationService.SendNotificationAsync(citizen.DeviceToken, title, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase Notification Failed: {ex.Message}");
            }
        }

        private static string GetStatusString(int status) => status switch
        {
            1 => "Pending",
            2 => "In Progress",
            3 => "Resolved",
            _ => "Updated"
        };

        private static string GetActivityMessage(string status) => status switch
        {
            "Pending" => "تم استلام البلاغ وهو في انتظار المراجعة.",
            "In Progress" => "البلاغ قيد المعالجة الآن بواسطة الجهة المختصة.",
            "Resolved" => "تم حل البلاغ وإغلاقه بنجاح.",
            _ => "تم تحديث حالة البلاغ."
        };
    }
}