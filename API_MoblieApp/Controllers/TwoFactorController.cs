using BLL.Service;
using Database.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SIRS_API.Controllers
{
    /// <summary>
    /// Handles Multi-Factor Authentication (MFA) including setup, enabling, and verification.
    /// </summary>
    [ApiController]
    [Route("api/2fa")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "citizen")]

    public class TwoFactorController : ControllerBase
    {
        private readonly OtpService _otp;
        private readonly QrCodeService _qr;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<TwoFactorController> _logger;

        private static readonly Dictionary<string, int> _attempts = new();

        public TwoFactorController(
            OtpService otp,
            QrCodeService qr,
            UserManager<ApplicationUser> users,
            ILogger<TwoFactorController> logger)
        {
            _otp = otp;
            _qr = qr;
            _users = users;
            _logger = logger;
        }

        /// <summary>
        /// Initiates 2FA setup by generating a secret key and a QR code.
        /// </summary>
        /// <remarks>
        /// This endpoint requires an authorized user. It updates the user's secret key in the database.
        /// </remarks>
        /// <response code="200">Returns the QR Code in Base64 format and the raw Secret Key.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpGet("Setup")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Setup()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var secret = _otp.GenerateSecretKey();
            user.TwoFactorSecret = secret;
            await _users.UpdateAsync(user);

            var qrImage = _qr.GenerateQrCodeBase64(user.Email, secret);
            return Ok(new { qrImage, secret });
        }

        /// <summary>
        /// Finalizes the 2FA enablement after verifying the first OTP code.
        /// </summary>
        /// <param name="code">The 6-digit code from the Authenticator app.</param>
        /// <response code="200">2FA successfully enabled.</response>
        /// <response code="400">If the provided code is invalid.</response>
        [HttpPost("Enable")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Enable([FromBody] string code)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (!_otp.VerifyOtp(code, user.TwoFactorSecret))
            {
                _logger.LogWarning("OTP activation failed for user {Email}", user.Email);
                return BadRequest(new { message = "Invalid OTP code" });
            }

            user.TwoFactorEnabled = true;
            await _users.UpdateAsync(user);
            return Ok(new { message = "2FA Enabled Successfully ✓" });
        }

        /// <summary>
        /// Verifies a 2FA code during the login process.
        /// </summary>
        /// <remarks>
        /// Includes brute-force protection: Maximum 3 failed attempts allowed per email.
        /// </remarks>
        /// <param name="req">The verification request containing Email and OTP Code.</param>
        /// <response code="200">OTP verified successfully.</response>
        /// <response code="401">Invalid code, user not found, or 2FA not enabled.</response>
        /// <response code="429">Too many failed attempts. User is temporarily blocked.</response>
        [HttpPost("Verify")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Verify([FromBody] VerifyRequest req)
        {
            var user = await _users.FindByEmailAsync(req.Email);
            if (user == null || !user.TwoFactorEnabled)
                return Unauthorized(new { message = "User not found or 2FA not enabled" });

            var attempts = _attempts.GetValueOrDefault(req.Email, 0);
            if (attempts >= 3)
            {
                _logger.LogWarning("Brute-force attempt detected for user {Email}", req.Email);
                return StatusCode(429, new { message = "Too many attempts. Please try again later." });
            }

            if (!_otp.VerifyOtp(req.Code, user.TwoFactorSecret))
            {
                _logger.LogWarning("OTP verification failed for user {Email}", req.Email);
                _attempts[req.Email] = attempts + 1;
                return Unauthorized(new { message = "Invalid or expired code" });
            }

            _attempts.Remove(req.Email);
            return Ok(new { message = "OTP Verified Successfully ✓" });
        }
    }

    /// <summary>
    /// Data transfer object for OTP verification.
    /// </summary>
    public record VerifyRequest(string Email, string Code);
}