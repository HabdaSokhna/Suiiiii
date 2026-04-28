using BLL.DTO.Authority;
using BLL.Managers.Authority;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SIRS_API.Controllers 
{
    [Route("api/[controller]")] 
    [ApiController]
    [ApiExplorerSettings(GroupName = "authority")]
    public class LoginAuthorityController : ControllerBase
    {
        private readonly ILoginAuthority _loginAuthority;

        public LoginAuthorityController(ILoginAuthority loginAuthority)
        {
            _loginAuthority = loginAuthority;
        }

        [ApiExplorerSettings(GroupName = "authority")]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] AuthorityLoginDto model)
        {
            var response = await _loginAuthority.LoginWithReportsAsync(model);

            if (response == null)
                return Unauthorized(new { message = "الإيميل أو كلمة السر غلط يسطا" });

            return Ok(response);
        }

        
        [Authorize(Roles = "Authority")]
        [HttpGet("GetMyReports")]
        private async Task<IActionResult> GetMyReports()
        {
            // سحب الإيميل من الـ Token
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
                return BadRequest("مش عارف أوصل لبياناتك من التوكن");

            var reports = await _loginAuthority.GetReportsAfterLoginAsync(email);
            return Ok(reports);
        }
    }
} 