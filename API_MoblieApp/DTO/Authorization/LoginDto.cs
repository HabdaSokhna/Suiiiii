using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SIRS_API.DTO.Authorization
{
    /// <summary>
    /// Data Transfer Object for user login.
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Email address or Egyptian phone number.
        /// </summary>
        /// <example>user@example.com</example>
        [Required]
        [MinLength(8, ErrorMessage = "الحد الأدنى 8 حروف")]
        [RegularExpression(@"^([\w\.\-]+@[\w\-]+\.[\w\-]+)|(01[0125][0-9]{8})$",
          ErrorMessage = "يجب إدخال بريد إلكتروني صحيح أو رقم هاتف مصري مكون من 11 رقم")]
        public string EmailorPhoneNumber { get; set; } = default!;

        /// <summary>
        /// User's secret password.
        /// </summary>
        /// <example>P@ssword123</example>
        [MinLength(8, ErrorMessage = "الحد الأدنى 8 حروف")]
        [Required(ErrorMessage = "يجب إدخال كلمة المرور")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = default!;
    }
}