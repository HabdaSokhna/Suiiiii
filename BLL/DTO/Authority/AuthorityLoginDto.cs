using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Authority
{
    public class AuthorityLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? DeviceToken { get; set; } // ✅ موجود
    }
}
