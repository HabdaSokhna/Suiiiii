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
        private readonly IAuthorityNotificationService _authorityNotif;

        public ReportsAuthorityController(
            Ai_Reports_Context context,
            INotificationService notificationService,
            IGeocodingService geocoding,
            ICitizenNotificationManager notificationManager,
            IAuthorityNotificationService authorityNotif)
        {
            _context = context;
            _notificationService = notificationService;
            _geocoding = geocoding;
            _notificationManager = notificationManager;
            _authorityNotif = authorityNotif;
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

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            // ✅ كل الريبورتات بدون فلترة شهر
            var reports = await _context.TbReport
                .AsNoTracking()
                .Where(r => !r.IsDeleted &&
                            r.Report_Category != null &&
                            r.Report_Category.Contains(authority.Authority.Category))
                .OrderByDescending(r => r.Report_Submit)
                .Select(r => new
                {
                    r.Report_ID,
                    r.Report_Submit,
                    CitizenName = r.Citizen != null ? r.Citizen.Citizen_Name : "Unknown Citizen",
                    Category = r.Report_Category ?? "Unclassified",
                    Status = r.LstHandle
                        .OrderByDescending(h => h.LastUpdated)
                        .Select(h => h.Status)
                        .FirstOrDefault() ?? "Pending",
                    Location = r.Report_GeoLocation,
                    DateTime = r.Report_Submit.ToString("dd/MM/yyyy HH:mm"),
                    ConfidenceRaw = r.Confidence_Score * 100,
                    Confidence = (r.Confidence_Score * 100).ToString("0.0") + "%"
                })
                .ToListAsync();

            // ✅ CountReportsInMonth من الـ list مش query جديدة
            var countReportsInMonth = reports
                .Count(r => r.Report_Submit >= startOfMonth && r.Report_Submit < endOfMonth);

            var result = new List<object>();
            foreach (var r in reports)
            {
                var address = await _geocoding.GetAddressAsync(r.Location);

                // ✅ Priority Logic المعدل
                string priority = r.ConfidenceRaw switch
                {
                    >= 85 => "High",    
                    >= 75 and < 85 => "Medium",  
                    >= 50 and < 75 => "Low",
                    _ => "Low"
                };

                result.Add(new
                {
                    r.Report_ID,
                    r.CitizenName,
                    r.Category,
                    r.Status,
                    Location = address,
                    RawLocation = r.Location,
                    r.DateTime,
                    r.Confidence,
                    Priority = priority
                });
            }

            return Ok(new
            {
                CountReportsInMonth = countReportsInMonth,  // ✅ عدد شهر الحالي
                TotalReports = reports.Count,               // ✅ إجمالي كل الريبورتات
                Reports = result
            });
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

            string status = report.LstHandle.Any()
                ? report.LstHandle
                    .OrderByDescending(h => h.Handle_ID)
                    .Select(h => h.Status)
                    .FirstOrDefault() ?? "Pending"
                : "Pending";

            var address = await _geocoding.GetAddressAsync(report.Report_GeoLocation);

            // ✅ Priority Logic
            var confidenceRaw = report.Confidence_Score * 100;
            string priority = confidenceRaw switch
            {
                >= 50 and < 70 => "Low",
                >= 75 and < 90 => "Medium",
                >= 90 => "High",
                _ => "Low"
            };

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
                Priority = priority,   // ✅
                AI_Analysis = new
                {
                    Predicted = report.AI_Category,
                    Score = $"{(report.Confidence_Score * 100f):0.#}%"
                },
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

            timeline.Add(new
            {
                StatusName = "Pending",
                CitizenName = citizenName,
                Time = reportData.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                Message = GetActivityMessage("Pending")
            });

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

        [HttpPut("UpdateStatus")]
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
                    .Include(r => r.LstHandle)
                    .FirstOrDefaultAsync(r => r.Report_ID == model.ReportId && !r.IsDeleted);

                if (report == null)
                    return NotFound(new { message = "البلاغ غير موجود." });

                int currentStatus = report.UpdatedStatus;

                // ✅ استبدال TimeZoneInfo بـ UtcNow لأن Linux Server مش بيعرف "Egypt Standard Time"
                var egyptTime = DateTime.UtcNow.AddHours(2);

                if (model.NewStatus < currentStatus)
                {
                    if (currentStatus == 3) report.Solved = null;
                    if (model.NewStatus == 1)
                        _context.TbHandle.RemoveRange(report.LstHandle);
                }
                else
                {
                    string statusText = GetStatusString(model.NewStatus);

                    _context.TbHandle.Add(new Handle
                    {
                        Report_ID = model.ReportId,
                        Authority_ID = currentAuthorityId,
                        Status = statusText,
                        LastUpdated = egyptTime
                    });

                    if (model.NewStatus == 3) report.Solved = egyptTime;
                }

                report.UpdatedStatus = model.NewStatus;

                int affected = await _context.SaveChangesAsync();

                // ✅ تأكد إن الحفظ نجح قبل ما تكمل
                if (affected == 0)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "لم يتم حفظ أي تغييرات في قاعدة البيانات." });
                }

                await transaction.CommitAsync();

                // ✅ await بدل fire-and-forget + try-catch منفصل لكل إشعار
                try
                {
                    await _notificationManager.FillAndSendAsync(
                        report.Citizen_ID, "ReportUpdate", GetStatusString(model.NewStatus));
                }
                catch (Exception notifEx)
                {
                    // الإشعار فشل لكن العملية الأساسية نجحت - سجل الخطأ فقط
                    Console.WriteLine($"[Notification Error - Citizen] {notifEx.Message}");
                }

                try
                {
                    await _authorityNotif.SendAsync(currentAuthorityId, "UpdateReport", model.ReportId);
                }
                catch (Exception notifEx)
                {
                    Console.WriteLine($"[Notification Error - Authority] {notifEx.Message}");
                }

                return Ok(new { success = true, message = "تم تسجيل التحديث الجديد بنجاح" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
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
            "Pending" => "The report has been received and is pending review.",
            "In Progress" => "The report is currently being processed by the relevant authority.",
            "Resolved" => "The report has been resolved and closed successfully.",
            _ => "The report status has been updated."
        };
    }
}