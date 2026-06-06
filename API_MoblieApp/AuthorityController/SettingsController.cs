using BLL.Service;
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
    public class SettingsController : ControllerBase
    {
        private readonly Ai_Reports_Context _context;

        public SettingsController(Ai_Reports_Context context)
        {
            _context = context;
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordAuthorityDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // جيب الـ Authority ID من التوكن
            var authIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authIdClaim) || !int.TryParse(authIdClaim, out int loginId))
                return Unauthorized(new { message = "هوية الجهة غير موجودة في التوكن." });

            var authLogin = await _context.TbAuthority_Login
                .FirstOrDefaultAsync(a => a.Login_ID == loginId);

            if (authLogin == null)
                return NotFound(new { message = "الحساب مش موجود." });

            // تحقق من الباسورد القديم
            if (authLogin.Password != model.OldPassword)
                return BadRequest(new { message = "كلمة المرور القديمة غلط." });

            // تحقق إن الباسورد الجديد مختلف
            if (model.OldPassword == model.NewPassword)
                return BadRequest(new { message = "كلمة المرور الجديدة لازم تكون مختلفة." });

            // غير الباسورد
            authLogin.Password = model.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تغيير كلمة المرور بنجاح ✓" });
        }
    }

    public class ChangePasswordAuthorityDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}