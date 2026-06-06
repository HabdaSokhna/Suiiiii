using BLL.DTO.Authorization;
using BLL.DTO.Responce;
using BLL.Mangers.CitizenAccount;
using BLL.Service;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace BLL.Controllers
{
    /// <summary>
    /// Manages citizen account operations including authentication, registration, and password management.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(GroupName = "citizen")]
    public class AccountController : ControllerBase
    {
        private readonly IRegisters _registerManager;
        private readonly ILogin _loginManager;
        private readonly OtpStore _otpStore;
        private readonly IOtp _otpManager;
        private readonly IForgetPassword _forgetPassword;
        private readonly EmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Ai_Reports_Context _context;

        public AccountController(
            IRegisters registerManager,
            ILogin loginManager,
            OtpStore otpStore,
            IOtp otpManager,
            IForgetPassword forgetPassword,
            EmailService emailService,
            UserManager<ApplicationUser> userManager,
            Ai_Reports_Context context)
        {
            _registerManager = registerManager;
            _loginManager = loginManager;
            _otpStore = otpStore;
            _otpManager = otpManager;
            _forgetPassword = forgetPassword;
            _emailService = emailService;
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Authenticates a citizen and returns an access token.
        /// </summary>
        /// <remarks>
        /// If the device token matches the stored one, a JWT token is returned immediately.
        /// Otherwise, an OTP is sent to the user's email for verification.
        ///
        ///     POST /api/Account/login
        ///     {
        ///         "emailorPhoneNumber": "user@example.com",
        ///         "password": "Password@123",
        ///         "deviceToken": "firebase-device-token"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Login credentials including Email/Phone, Password, and Device Token.</param>
        /// <returns>JWT token and user details, or OTP required flag.</returns>
        /// <response code="200">Login successful. Returns JWT token and user info.</response>
        /// <response code="401">Invalid credentials provided.</response>
        /// <response code="400">Validation failed.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthorizationResponceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _loginManager.ExecuteAsync(model);

            if (!result.IsSuccess)
                return Unauthorized(new { message = result.Message });

            return Ok(result);
        }

        /// <summary>
        /// Registers a new citizen account and sends an OTP for email verification.
        /// </summary>
        /// <remarks>
        /// After successful registration, an OTP is sent to the provided email.
        /// The account remains inactive until the OTP is verified via /verify-otp.
        /// If the same email registers again without verifying, the old data is deleted and a new OTP is sent.
        ///
        ///     POST /api/Account/register
        ///     {
        ///         "fullName": "John Doe",
        ///         "email": "user@example.com",
        ///         "password": "Password@123",
        ///         "confirmPassword": "Password@123",
        ///         "phoneNumber": "01012345678",
        ///         "nationalId": "12345678901234",
        ///         "deviceToken": "firebase-device-token"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Registration details including personal info and credentials.</param>
        /// <returns>Success confirmation message.</returns>
        /// <response code="200">Account created. OTP sent to email.</response>
        /// <response code="400">Validation failed or email already exists and is verified.</response>
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

        /// <summary>
        /// Verifies the OTP code sent to the user's email.
        /// </summary>
        /// <remarks>
        /// Used for two cases:
        /// 1. After Register → activates the account and returns a JWT token.
        /// 2. After Login with a new device → verifies identity and returns a JWT token.
        ///
        /// If the OTP is wrong or expired during registration, all account data is deleted automatically.
        ///
        ///     POST /api/Account/verify-otp
        ///     {
        ///         "email": "user@example.com",
        ///         "code": "123456",
        ///         "deviceToken": "firebase-device-token"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Email, OTP code, and device token.</param>
        /// <returns>JWT token, role, and user info on success.</returns>
        /// <response code="200">OTP verified. Returns JWT token and user details.</response>
        /// <response code="401">OTP is incorrect or has expired.</response>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto model)
        {
            var result = await _otpManager.VerifyAsync(model.Email, model.Code, model.DeviceToken, model.Purpose);

            if (!result.IsSuccess)
                return Unauthorized(new { message = result.Message });

            return Ok(new AuthorizationResponceDto
            {
                IsSuccess = true,
                Message = result.Message,
                Token = result.Token,
                Expires = result.Expires,
                Role = result.Role,
                UserName = result.UserName,
                CitizenId = result.CitizenId
            });
        }

        /// <summary>
        /// Sends an OTP to the user's email to initiate the password reset process.
        /// </summary>
        /// <remarks>
        /// For security reasons, the response is always successful even if the email does not exist.
        ///
        ///     POST /api/Account/forgot-password
        ///     {
        ///         "email": "user@example.com"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">The email address associated with the account.</param>
        /// <returns>Confirmation message that OTP was sent.</returns>
        /// <response code="200">OTP sent if the email exists in the system.</response>
        /// <response code="400">Validation failed.</response>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _forgetPassword.SendOtpAsync(model);
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// Resets the user's password after verifying the OTP.
        /// </summary>
        /// <remarks>
        /// The OTP must be the one sent via /forgot-password and must not be expired (valid for 5 minutes).
        ///
        ///     POST /api/Account/reset-password
        ///     {
        ///         "email": "user@example.com",
        ///         "code": "123456",
        ///         "newPassword": "NewPassword@123"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Email, OTP code, and the new password.</param>
        /// <returns>Success or failure message.</returns>
        /// <response code="200">Password reset successfully.</response>
        /// <response code="400">OTP is incorrect, expired, or password does not meet requirements.</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _forgetPassword.ResetPasswordAsync(model);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
    }
}