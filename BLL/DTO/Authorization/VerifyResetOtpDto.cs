using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Authorization
{
    public class VerifyResetOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
