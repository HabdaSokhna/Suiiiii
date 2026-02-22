using Database;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRS_API.Controllers;
using SIRS_API.DTO.Authorization;
using SIRS_API.Services;


[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly Ai_Reports_Context _context;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager, ITokenService tokenService, Ai_Reports_Context context, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
        _signInManager = signInManager;
    }

    private async Task<Notification> SendNotificationAsync(int citizenId, string title, string message, string type)
    {
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
        return notification;
    }
    /// <summary>
    /// Registers a new citizen user in the system.
    /// </summary>
    /// <remarks>
    /// This process involves creating an Identity user, assigning the "Citizen" role, 
    /// and initializing a citizen profile with their national ID and phone number.
    /// </remarks>
    /// <param name="model">The registration data transfer object.</param>
    /// <returns>A success message and the initial welcome notification.</returns>
    /// <response code="200">User created successfully.</response>
    /// <response code="400">Email or National ID already exists, or validation failed.</response>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _userManager.FindByEmailAsync(model.Email) != null)
            return BadRequest(new { message = "البريد الإلكتروني مستخدم بالفعل." });

        if (await _context.TbCitizen.AnyAsync(c => c.Citizen_National_Id == model.NationalId))
            return BadRequest(new { message = "الرقم القومي مسجل مسبقاً." });

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "Citizen");

            var citizenProfile = new Citizen
            {
                ApplicationUserId = user.Id,
                Citizen_Name = model.FullName,
                Citizen_Email = model.Email,
                Citizen_National_Id = model.NationalId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            citizenProfile.LstPhone.Add(new Citizen_Phone { Phone_Number = model.PhoneNumber });
            _context.TbCitizen.Add(citizenProfile);
            await _context.SaveChangesAsync();

            // إرسال الإشعار
            var notif = await SendNotificationAsync(citizenProfile.Citizen_ID, "أهلاً بك", "تم إنشاء حسابك بنجاح.", "system");

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "تم إنشاء الحساب بنجاح.",
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "خطأ في السيرفر", error = ex.Message });
        }
    }
    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <remarks>
    /// Users can login using either their email address or their registered phone number.
    /// </remarks>
    /// <param name="model">Login credentials.</param>
    /// <returns>JWT token, expiry date, and a login alert notification.</returns>
    /// <response code="200">Authentication successful.</response>
    /// <response code="401">Invalid credentials provided.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        ApplicationUser? user = model.EmailorPhoneNumber.Contains("@")
            ? await _userManager.FindByEmailAsync(model.EmailorPhoneNumber)
            : await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.EmailorPhoneNumber);

        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized(new { message = "بيانات الدخول غير صحيحة." });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        var citizen = await _context.TbCitizen.FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

        if (citizen != null)
        {
            
            await FillNotificationTable(citizen.Citizen_ID, "Login");
        }
        return Ok(new
        {
            token = token,
            expires = DateTime.UtcNow.AddDays(1),
        });
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