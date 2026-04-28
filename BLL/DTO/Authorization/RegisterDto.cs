using System.ComponentModel.DataAnnotations;

namespace BLL.DTO.Authorization
{
    /// <summary>
    /// Data Transfer Object for creating a new user account.
    /// </summary>
    public class RegisterDto
    {
        /// <summary>
        /// Full name of the user.
        /// </summary>
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 100 حرف")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the user
        ///</summary>

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [StringLength(100, ErrorMessage = "الإيميل طويل جداً")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// New password for the account.
        /// </summary>
        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Password confirmation field.
        /// </summary>
        /// <example>NewStrongP@ss2026</example>
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Egyptian phone number.
        /// </summary>
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "رقم الهاتف المصري غير صحيح، يجب أن يبدأ بـ 010 أو 011 أو 012 أو 015 ويتكون من 11 رقم")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 14-digit Egyptian National ID.
        /// </summary>
        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [RegularExpression(@"^[23]\d{13}$", ErrorMessage = "الرقم القومي المصري يجب أن يبدأ بـ 2 أو 3 ومكون من 14 رقمًا")]
        [StringLength(14, MinimumLength = 14)]
        public string NationalId { get; set; } = string.Empty;
        public string? DeviceToken { get; set; }
    }
}