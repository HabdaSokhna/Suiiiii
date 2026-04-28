using System.ComponentModel.DataAnnotations;

namespace BLL.DTO.User
{
    /// <summary>
    /// Data Transfer Object for secure password updates.
    /// Implements double-confirmation for the new password to prevent entry errors.
    /// </summary>
    public class ChangePassword_Dto
    {
        /// <summary>
        /// The user's existing password required for identity verification.
        /// </summary>
        /// <example>OldP@ss123</example>
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// The new password following security policy: 
        /// Minimum 8 characters, including mixed cases and special characters (if configured in Identity).
        /// </summary>
        /// <example>NewStrongP@ss2026</example>
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Re-entry of the new password to ensure consistency. 
        /// Must match 'NewPassword' exactly.
        /// </summary>
        /// <example>NewStrongP@ss2026</example>
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}