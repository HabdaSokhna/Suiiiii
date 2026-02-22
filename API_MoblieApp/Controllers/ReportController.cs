using CURD;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using SIRS_API.DTO.Ai_Model;
using SIRS_API.DTO.Notification;
using SIRS_API.DTO.Report;
using System.Security.Claims;

namespace SIRS_API.Controllers
{
    /// <summary>
    /// Controller responsible for managing all report-related operations including creation, 
    /// history tracking, and detailed status viewing.
    /// Requires Bearer Token Authentication for all endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportRepository _reportRepo;
        private readonly ICitizenRepository _citizenRepo;
        private readonly IWebHostEnvironment _environment;
        private readonly Ai_Reports_Context _context;
        private readonly YoloService _yoloService;

        public ReportsController(
            IReportRepository reportRepo,
            ICitizenRepository citizenRepo,
            IWebHostEnvironment environment,
            Ai_Reports_Context context,
            YoloService yoloService)
        {
            _reportRepo = reportRepo;
            _citizenRepo = citizenRepo;
            _environment = environment;
            _context = context;
            _yoloService = yoloService;
        }

        /// <summary>
        /// Submits a new citizen report including description, geolocation, and optional photo.
        /// </summary>
        /// <param name="model">Form-data containing Title, Description, Location, Category, and Photo file.</param>
        /// <returns>A 201 Created response with the generated Report ID and the route to access it.</returns>
        [HttpPost("CreateReport")]
        public async Task<IActionResult> CreateReport([FromForm] ReportCreate_Dto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "بيانات البلاغ غير صالحة", errors = ModelState });

            try
            {
                // 1. استخراج الهوية من التوكن
                var userEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email)
                                ?? User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "فشل التعرف على المستخدم من التوكن" });

                // 2. جلب ملف المواطن
                var citizen = await _citizenRepo.GetByEmailAsync(userEmail);
                if (citizen == null)
                    return NotFound(new { message = "لم يتم العثور على ملف مواطن لهذا الحساب" });

                // --- [بداية منطق الـ AI] ---
                PredictionResult_Dto aiResult = new PredictionResult_Dto(); // قيم افتراضية
                string? photoPath = null;

                if (model.Photo != null && model.Photo.Length > 0)
                {
                    // أ. تحويل الصورة لـ Bytes عشان الـ AI يحللها
                    using var ms = new MemoryStream();
                    await model.Photo.CopyToAsync(ms);
                    byte[] imageBytes = ms.ToArray();

                    // ب. استدعاء خدمة الـ YOLO لتحليل الصورة
                    aiResult = _yoloService.AnalyzeImage(imageBytes);

                    // ج. حفظ الصورة فعلياً في السيرفر (بعد التحليل)
                    photoPath = await SavePhotoAsync(model.Photo);
                }
                // --- [نهاية منطق الـ AI] ---

                // 4. إنشاء كيان البلاغ (بالبيانات المطعمة من الـ AI)
                var report = new Report
                {
                    Report_Description = $"Title: {model.Title}\nDescription: {model.Description}",
                    Report_GeoLocation = model.Location,

                    // لو المستخدم مبعتش تصنيف، ناخد تصنيف الـ AI
                    Report_Category = string.IsNullOrEmpty(model.Category) || model.Category == "string"
                                       ? aiResult.Tag
                                       : model.Category,

                    PhotoPath = photoPath,
                    Report_Submit = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    Citizen_ID = citizen.Citizen_ID,

                    // تخزين نسبة تأكد الـ AI في الداتابيز
                    Confidence_Score = aiResult.Confidence
                };

                // 5. حفظ البلاغ في الداتابيز
                var createdReport = await _reportRepo.CreateAsync(report);
                await FillNotificationTable(citizen.Citizen_ID, "CreateReport");

                // 6. [NOTIFICATION] إرسال الإشعار وتخزين النتيجة (باستخدام التصنيف النهائي)
                var notif = await SendNotificationAsync(
                    citizen.Citizen_ID,
                    "تم استلام بلاغك",
                    $"نشكرك على تعاونك. تم تسجيل بلاغك بنجاح تحت تصنيف ({report.Report_Category}) وجاري المراجعة.",
                    "report"
                );
            
                // 7. الرد النهائي
                return CreatedAtRoute(
                    "GetReportById",
                    new { id = createdReport.Report_ID }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطأ داخلي في السيرفر", detail = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves paginated report history for the authenticated citizen with optional filtering.
        /// </summary>
        /// <param name="category">Filter by report category (e.g., Water, Electricity).</param>
        /// <param name="status">Filter by current processing status (Pending, Resolved, etc.).</param>
        /// <param name="fromDate">Filter records starting from this date.</param>
        /// <param name="toDate">Filter records up to this date.</param>
        /// <param name="page">The current page number (defaults to 1).</param>
        /// <param name="pageSize">Number of records per page (defaults to 10).</param>
        [HttpGet("History")]
        public async Task<IActionResult> GetReportHistory(
    [FromQuery] string? category = null,
    [FromQuery] string? status = null,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            try
            {
                // 1. استخراج الـ User ID من التوكن
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                // 2. البحث عن المواطن
                var citizen = await _context.TbCitizen
                    .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && !c.IsDeleted);

                if (citizen == null) return NotFound(new { message = "Citizen profile not found" });

                // 3. بناء الاستعلام والبحث بـ Citizen_ID
                // ملحوظة: بما إن مفيش Handle_ID، هنرتب بالـ Report_ID جوه الـ Select لضمان الـ Deterministic Order
                var query = _context.TbReport
                    .Where(r => r.Citizen_ID == citizen.Citizen_ID && !r.IsDeleted)
                    .Select(r => new {
                        Report = r,
                        // بنجيب آخر حالة عن طريق الترتيب العكسي للـ Report_ID داخل جدول الـ Handle
                        CurrentStatus = r.LstHandle
                            .OrderByDescending(h => h.Report_ID)
                            .Select(h => h.Status)
                            .FirstOrDefault() ?? "Pending"
                    });

                // 4. تطبيق الفلاتر (Status, Category, Dates)
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(q => q.CurrentStatus == status);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(q => q.Report.Report_Category == category);

                if (fromDate.HasValue)
                    query = query.Where(q => q.Report.Report_Submit >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(q => q.Report.Report_Submit <= toDate.Value);

                // 5. حساب الإجمالي والـ Pagination
                var totalCount = await query.CountAsync();
                var reportsData = await query
                    .OrderByDescending(q => q.Report.Report_Submit)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 6. تحويل البيانات لـ DTO (Mapping يدوي لمنع الـ Cycle)
                var history = reportsData.Select(q => new Report_Dto
                {
                    Report_ID = q.Report.Report_ID,
                    Category = q.Report.Report_Category,
                    SubmittedAt = q.Report.Report_Submit,
                    Status = q.CurrentStatus,
                    Title = q.Report.Report_Description.Split('\n').FirstOrDefault()?.Replace("Title: ", "").Trim() ?? "",
                    Photo = !string.IsNullOrEmpty(q.Report.PhotoPath)
                        ? $"{Request.Scheme}://{Request.Host}{q.Report.PhotoPath}"
                        : null
                }).ToList();

                // 7. [NOTIFICATION] جلب أحدث إشعار للمواطن
                var lastNotif = await _context.TbNotification
                    .Where(n => n.Citizen_ID == citizen.Citizen_ID)
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync();

                // 8. الرد النهائي
                return Ok(new
                {
                    success = true,
                    data = history,
                    pagination = new
                    {
                        totalCount,
                        page,
                        pageSize,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "حدث خطأ أثناء معالجة طلبك",
                    detail = ex.Message
                });
            }
        }
        /// <summary>
        /// Retrieves the full details of a specific report, including its audit trail (Handles).
        /// </summary>
        /// <param name="id">The unique identifier of the report.</param>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("{id}", Name = "GetReportById")]
        public async Task<IActionResult> GetReportById(int id)
        {
            try
            {
                // Fetch report with related Citizen and full history of processing handles
                var report = await _context.TbReport
                    .Include(r => r.Citizen)
                    .Include(r => r.LstHandle)
                        .ThenInclude(h => h.Authority)
                    .FirstOrDefaultAsync(r => r.Report_ID == id && !r.IsDeleted);

                if (report == null)
                    return NotFound(new { success = false, message = "Report not found" });

                var userEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? User.FindFirstValue(ClaimTypes.Email);
                var userRole = User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role);

                // Security Policy: Admins can see all, Citizens can only see their own reports
                if (userRole != "Admin" && report.Citizen?.Citizen_Email != userEmail)
                {
                    return Forbid();
                }

                // Get the most recent status update
                var latestHandle = report.LstHandle?
                    .OrderByDescending(h => h.LastUpdated)
                    .FirstOrDefault();

                // Build the comprehensive Read DTO
                var dto = new ReportRead_Dto
                {
                    Report_ID = report.Report_ID,
                    Report_Description = report.Report_Description,
                    Report_GeoLocation = report.Report_GeoLocation,
                    Report_Submit = report.Report_Submit,
                    Report_Category = report.Report_Category,
                    Report_PredictedCategory = report.Report_PredictedCategory,
                    PhotoPath = !string.IsNullOrEmpty(report.PhotoPath)
                        ? $"{Request.Scheme}://{Request.Host}{report.PhotoPath}"
                        : null,
                    Confidence_Score = report.Confidence_Score,
                    CreatedAt = report.CreatedAt,
                    CitizenName = report.Citizen?.Citizen_Name ?? "Unknown",
                    CitizenEmail = report.Citizen?.Citizen_Email ?? ""
                };

                return Ok(new
                {
                    success = true,
                    status = latestHandle?.Status ?? "Pending",
                    data = dto,
                    // Map the timeline of authorities who handled the report
                    handles = report.LstHandle?.Select(h => new
                    {
                        authorityName = h.Authority?.Authority_Name ?? "",
                        department = h.Authority?.Department_Name ?? "",
                        status = h.Status,
                        lastUpdated = h.LastUpdated
                    }).OrderByDescending(h => h.lastUpdated).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Details retrieval error", detail = ex.Message });
            }
        }

        /// <summary>
        /// Internal helper to save uploaded image files to the physical server storage.
        /// </summary>
        /// <param name="photo">The image file from the request.</param>
        /// <returns>Relative path string of the saved file or null on failure.</returns>
        private async Task<string?> SavePhotoAsync(IFormFile photo)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "reports");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate a unique GUID filename to prevent collisions
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(photo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                return $"/uploads/reports/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Error in SavePhotoAsync: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// الميثود المساعدة لإرسال الإشعارات (تأكد من وجودها في نفس الكنترولر أو كخدمة مشتركة)
        /// </summary>
        [NonAction]
        private async Task<NotificationResponse_Dto> SendNotificationAsync(int citizenId, string title, string message, string type)
        {
            var notification = new Notification
            {
                Citizen_ID = citizenId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            // بافتراض استخدام الـ Context مباشرة هنا أو عبر ريبوزيتوري الإشعارات
            _context.TbNotification.Add(notification);
            await _context.SaveChangesAsync();

            return new NotificationResponse_Dto
            {
                Id = notification.Notification_ID.ToString(),
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt
            };
        }
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