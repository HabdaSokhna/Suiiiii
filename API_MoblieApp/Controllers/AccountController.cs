using BLL.DTO.Authorization;
using BLL.DTO.Responce;
using BLL.Mangers.CitizenAccount;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace BLL.Controllers
{
    /// <summary>
    /// Manages citizen account operations, including registration and authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(GroupName = "citizen")]
    public class AccountController : ControllerBase
    {
        private readonly IRegisters _registerManager;
        private readonly ILogin _loginManager;

        public AccountController(IRegisters registerManager, ILogin loginManager)
        {
            _registerManager = registerManager;
            _loginManager = loginManager;
        }

        /// <summary>
        /// Authenticates a citizen and returns an access token.
        /// </summary>
        /// <param name="model">Login credentials (Email/Phone, Password, and Firebase Device Token).</param>
        /// <returns>An authorization response containing the JWT and user details.</returns>
        /// <response code="200">Login successful.</response>
        /// <response code="401">Invalid credentials provided.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthorizationResponceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _loginManager.ExecuteAsync(model);

            if (!result.IsSuccess)
                return Unauthorized(new { message = result.Message });

            return Ok(result);
        }

        /// <summary>
        /// Registers a new citizen account in the system.
        /// </summary>
        /// <param name="model">Detailed registration information.</param>
        /// <returns>Success confirmation or a list of validation errors.</returns>
        /// <response code="200">Registration successful.</response>
        /// <response code="400">Validation failed or user already exists.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _registerManager.ExecuteAsync(model);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message, errors = result.Errors });

            return Ok(result);
        }
    }
}