namespace BLL.DTO.Authorization
{
    public class VerifyOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? DeviceToken { get; set; }
         /// <summary>
        /// القيم المسموح بيها: Login / Register / ForgotPassword
        /// </summary>
        /// <example>Login</example>
      
        public string Purpose { get; set; } = "Login"; // Login / ForgotPassword
    }
}