using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.DTO.User;
using BLL.Managers.User;
using System.Security.Claims;
using System.Net.Mime;

namespace BLL.Controllers
{
    /// <summary>
    /// Handles citizen profile management, including security updates and status tracking.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(GroupName = "citizen")]
    public class UserController : ControllerBase
    {
        private readonly IProfileManager _profileManager;
        private readonly IWebHostEnvironment _env;

        public UserController(IProfileManager profileManager, IWebHostEnvironment env)
        {
            _profileManager = profileManager;
            _env = env;
        }

        /// <summary>
        /// Updates the email address for the authenticated user.
        /// </summary>
        /// <param name="model">The new email and current password for verification.</param>
        /// <returns>A status message indicating success.</returns>
        /// <response code="200">Email updated successfully.</response>
        /// <response code="400">Update failed (invalid password or email already in use).</response>
        [HttpPut("ChangeEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmail_Dto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _profileManager.ChangeEmailAsync(GetUserId(), model);

            if (!result)
                return BadRequest(new { message = "Email update failed. Please verify your current password." });

            return Ok(new { success = true, message = "Email updated successfully." });
        }

        /// <summary>
        /// Changes the password for the current account.
        /// </summary>
        /// <param name="model">Current and new password details.</param>
        /// <returns>A status message indicating success.</returns>
        /// <response code="200">Password changed successfully.</response>
        /// <response code="400">Password change failed due to invalid current password or complexity rules.</response>
        [HttpPost("ChangePassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword_Dto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _profileManager.ChangePasswordAsync(GetUserId(), model);

            if (!result)
                return BadRequest(new { message = "Password change failed." });

            return Ok(new { success = true, message = "Password updated successfully." });
        }

        /// <summary>
        /// Uploads and updates the user's profile picture.
        /// </summary>
        /// <param name="file">The image file (JPG, PNG).</param>
        /// <returns>The public URL of the uploaded photo.</returns>
        /// <response code="200">Photo uploaded successfully.</response>
        /// <response code="400">Invalid file format or upload error.</response>
        [HttpPost("UploadPhoto")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Invalid image file." });

            var photoPath = await _profileManager.UploadPhotoAsync(GetUserId(), file, _env.WebRootPath);

            if (photoPath == null)
                return BadRequest(new { message = "Photo upload failed." });

            var fullPath = $"{Request.Scheme}://{Request.Host}{photoPath}";
            return Ok(new { success = true, photoPath = fullPath, message = "Profile picture updated." });
        }

        /// <summary>
        /// Retrieves core profile information for the authenticated user.
        /// </summary>
        /// <returns>The user's full name, email, and photo URL.</returns>
        /// <response code="200">Returns the user profile data.</response>
        /// <response code="404">Profile not found.</response>
        [HttpGet("GetProfile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _profileManager.GetProfileAsync(GetUserId(), GetBaseUrl());

            if (profile == null)
                return NotFound(new { message = "Profile not found." });

            return Ok(profile);
        }

        /// <summary>
        /// Gets statistics regarding the user's incident reports (Total, Pending, In-Progress, Resolved).
        /// </summary>
        /// <returns>Summary statistics for user activity.</returns>
        /// <response code="200">Returns user report statistics.</response>
        /// <response code="401">Unauthorized access.</response>
        [HttpGet("GetUserStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserStatus()
        {
            var status = await _profileManager.GetUserStatusAsync(GetUserId(), GetBaseUrl());

            if (status == null)
                return NotFound(new { message = "Could not retrieve user status." });

            return Ok(status);
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}";
    }
}