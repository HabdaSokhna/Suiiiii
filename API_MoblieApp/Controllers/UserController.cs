using Database;
using Database.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRS_API.DTO.User;
using SIRS_API.Services;
using System.Security.Claims;

namespace SIRS_API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly Ai_Reports_Context _context;
        public UserController(UserManager<ApplicationUser> userManager, ITokenService tokenService, Ai_Reports_Context context)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
        }
        /// <summary>
        /// Updates the authenticated user's email address.
        /// </summary>
        /// <remarks>
        /// <b>Conditions:</b>
        /// - The user must provide their current password for verification.
        /// - The new email must be unique and not associated with another account.
        /// - After a successful change, the user's Identity UserName is also updated to the new email.
        /// </remarks>
        /// <param name="model">Contains NewEmail and CurrentPassword.</param>
        /// <response code="200">Success: Email updated successfully.</response>
        /// <response code="400">Bad Request: Invalid password or email already in use.</response>
        /// <response code="401">Unauthorized: Missing or invalid token.</response>
        [HttpPut("ChangeEmail")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmail_Dto model)
        {
            // 1. التحقق من هوية المستخدم
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                return BadRequest(new { message = "كلمة المرور غير صحيحة." });

            // 2. تحديث البريد الإلكتروني
            user.Email = model.NewEmail;
            user.UserName = model.NewEmail;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded) return BadRequest(result.Errors);

            // 3. [NOTIFICATION]
           
            var citizen = await _context.TbCitizen.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (citizen != null)
            {
                await FillNotificationTable(citizen.Citizen_ID, "ChangeEmail");
            }

            // 4. الرد النهائي (التعديل الجوهري هنا لمنع الانفجار)
            return Ok(new
            {
                success = true,
                message = "تم تحديث البريد الإلكتروني بنجاح.",
                
            });
        }
        /// <summary>
        /// Changes the password for the currently logged-in user.
        /// </summary>
        /// <remarks>
        /// <b>Security Note:</b>
        /// Upon success, the user's Security Stamp is updated, which invalidates all existing tokens to ensure security.
        /// </remarks>
        /// <param name="model">Contains CurrentPassword and NewPassword.</param>
        /// <response code="200">Success: Password has been changed.</response>
        /// <response code="400">Bad Request: Password requirements not met or incorrect current password.</response>
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword_Dto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. استخراج الهوية والتحقق من وجود المستخدم
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "المستخدم غير موجود." });

            // 2. تغيير كلمة المرور
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // تحديث الـ Security Stamp لضمان تسجيل خروج الجلسات الأخرى (زيادة أمان)
            await _userManager.UpdateSecurityStampAsync(user);

            // 3. [NOTIFICATION] إرسال الإشعار وتجهيزه للـ Response
            object? notificationResponse = null;
            var citizen = await _context.TbCitizen.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (citizen != null)
            {
                await FillNotificationTable(citizen.Citizen_ID, "ChangeEmail");


            }

            // 4. الرد النهائي
            return Ok(new
            {
                success = true,
                message = "تم تغيير كلمة المرور بنجاح.",
                notification = notificationResponse
            });
        }

        /// <summary>
        /// Uploads and sets a profile picture for the authenticated user.
        /// </summary>
        /// <remarks>
        /// <b>Process:</b>
        /// 1. Validates the file existence and checks for allowed extensions (.jpg, .jpeg, .png).
        /// 2. Saves the file with a unique GUID to 'wwwroot/Uploads/Profiles'.
        /// 3. Deletes the previous profile photo from the server to optimize storage.
        /// 4. Updates the 'ProfilePhotoPath' in the Identity User table.
        /// </remarks>
        /// <param name="file">The image file transmitted via 'multipart/form-data'.</param>
        /// <returns>A JSON object containing the success message and the new relative web path of the photo.</returns>
        /// <response code="200">Photo uploaded successfully and database updated.</response>
        /// <response code="400">Bad Request: File is missing or has an invalid extension.</response>
        /// <response code="401">Unauthorized: Missing or invalid authentication token.</response>
        /// <response code="500">Internal Server Error: Unexpected error during file saving or database update.</response>
        [HttpPost("UploadPhoto")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            // 1. التحقق من وجود الملف
            if (file == null || file.Length == 0) return BadRequest(new { message = "الصورة غير صالحة." });

            // 2. التحقق من هوية المستخدم
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "المستخدم غير موجود." });

            // 3. معالجة وحفظ الملف
            var extension = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Uploads/Profiles");

            // التأكد من أن المجلد موجود بالفعل
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var path = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. تحديث رابط الصورة في قاعدة البيانات
            user.ProfilePhotoPath = $"/Uploads/Profiles/{fileName}";
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return BadRequest(updateResult.Errors);

            // 5. [NOTIFICATION] إرسال الإشعار وتجهيزه للرد
            object? notificationResponse = null;
            var citizen = await _context.TbCitizen.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (citizen != null)
            {
                await FillNotificationTable(citizen.Citizen_ID, "ChangeEmail");
            }

            // 6. الرد النهائي
            return Ok(new
            {
                success = true,
                photoPath = user.ProfilePhotoPath,
                message = "تم رفع الصورة وتحديث الملف الشخصي.",
                notification = notificationResponse
            });
        }
        /// <summary>
        /// Retrieves core profile information for the currently logged-in user.
        /// </summary>
        /// <remarks>
        /// This endpoint performs a 'JOIN' (Include) between the Identity Users table and the Citizen profile table 
        /// to fetch data from both domains in a single database round-trip.
        /// </remarks>
        /// <returns>
        /// A profile object containing:
        /// - fullName: From the Citizen table.
        /// - email: From the Identity User table.
        /// - photo: Path to the image; returns a default avatar path if null.
        /// </returns>
        /// <response code="200">Returns the user profile successfully.</response>
        /// <response code="401">Unauthorized: Token validation failed.</response>
        /// <response code="404">Not Found: User exists but Citizen profile record is missing.</response>

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            // 1. استخراج الـ ID الخاص بالمستخدم من التوكن (الـ Claim)
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "فشلت عملية التحقق من الهوية." });

            // 2. جلب المستخدم من قاعدة البيانات مع جلب ملف المواطن المرتبط به (Include)
            var user = await _userManager.Users
        .Include(u => u.CitizenProfile) // الربط بين AspNetUsers و TbCitizen
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود." });

            // 3. التحقق من وجود ملف مواطن مرتبط
            if (user.CitizenProfile == null)
                return NotFound(new { message = "لم يتم العثور على بيانات بروفايل لهذا الحساب." });

            // 4. إرجاع البيانات المطلوبة فقط (DTO-like response)
            return Ok(new
            {
                fullName = user.CitizenProfile.Citizen_Name,
                email = user.Email,
                photo = user.ProfilePhotoPath ?? "/Uploads/Profiles/default-avatar.png" // صورة افتراضية لو لم يرفع صورة
            });
        }

        /// <summary>
        /// Retrieves the current user's dashboard statistics and profile summary.
        /// </summary>
        /// <remarks>
        /// This endpoint performs a deep inclusion across four entities:
        /// 1. ApplicationUser: To get the profile photo.
        /// 2. CitizenProfile: To get the full name.
        /// 3. LstReport: To get the total count of reports.
        /// 4. LstHandle: To determine the status of each report based on authority processing.
        /// 
        /// <b>Logic:</b> A report is counted in a status category if <i>any</i> authority handling that report has assigned it that specific status.
        /// </remarks>
        /// <returns>
        /// An object containing:
        /// - fullName: The Citizen's registered name.
        /// - photo: The URL path to the profile picture.
        /// - totalReports: Aggregate count of all submitted reports.
        /// - pendingCount: Count of reports currently in 'Pending' status.
        /// - inProgressCount: Count of reports currently being handled.
        /// - solvedCount: Count of successfully resolved reports.
        /// </returns>
        /// <response code="200">Statistics retrieved successfully.</response>
        /// <response code="401">Unauthorized: Token is missing or invalid.</response>
        /// <response code="404">Not Found: User or associated Citizen profile does not exist.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("GetUserStatus")]
        public async Task<IActionResult> GetUserStatus()
        {
            // 1. استخراج معرف المستخدم من التوكن (JWT Claim)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            // 2. جلب بيانات المستخدم مع بروفايل المواطن وتقاريره وحالاتها
            // نستخدم Include و ThenInclude لضمان تحميل البيانات من جداول الربط (Eager Loading)
            var user = await _userManager.Users
                .Include(u => u.CitizenProfile)
                    .ThenInclude(c => c.LstReport)
                        .ThenInclude(r => r.LstHandle)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.CitizenProfile == null)
                return NotFound(new { message = "المستخدم غير موجود أو لم يكمل ملفه الشخصي." });

            var reports = user.CitizenProfile.LstReport;

            // 3. تحليل الحالات (Logic): 
            // بما أن جدول Handle جسر وبدون ID، سنعتمد على أن آخر حالة في المصفوفة هي الأحدث
            var stats = reports.Select(r => {
                var lastHandle = r.LstHandle.LastOrDefault();
                return lastHandle?.Status; // سيرجع null إذا كان البلاغ Pending
            }).ToList();

            // 4. بناء كائن النتائج النهائي (The Response Object)
            var statusCounts = new
            {
                fullName = user.CitizenProfile.Citizen_Name,
                photo = user.ProfilePhotoPath ?? "/Uploads/Profiles/default-avatar.png",

                // إجمالي البلاغات المقدمة من هذا المواطن
                totalReports = reports.Count,

                // حساب عدد البلاغات لكل حالة
                pendingCount = stats.Count(s => s == null),
                inProgressCount = stats.Count(s => s == "In Progress"),
                resolvedCount = stats.Count(s => s == "Resolved")
            };

            return Ok(statusCounts);
        }
        private async Task<Notification> SendNotificationAsync(int citizenId, string title, string message, string type)
        {
            var notification = new Notification
            {
                Citizen_ID = citizenId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow // التخزين بالوقت الحالي
            };

            _context.TbNotification.Add(notification);
            await _context.SaveChangesAsync(); // هنا الـ ID والوقت بيتحفظوا فعلياً

            return notification; // السطر ده هو اللي "بيرجع" الإشعار بالوقت بتاعه بعد ما اتسيف
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
