using System.ComponentModel.DataAnnotations;

namespace BLL.DTO.User
{
    /// <summary>
    /// Data Transfer Object for updating the user's primary email address.
    /// Requires re-authentication via the current password for security.
    /// </summary>
    public class ChangeEmail_Dto
    {
        /// <summary>
        /// The new email address the user wishes to associate with their account.
        /// Must be a valid email format and unique within the system.
        /// </summary>
        /// <example>new.user@example.com</example>
        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string NewEmail { get; set; } = string.Empty;

        /// <summary>
        /// The user's current password to authorize this sensitive change.
        /// </summary>
        /// <example>P@ssw0rd123</example>
        [Required(ErrorMessage = "Current password is required for confirmation")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}