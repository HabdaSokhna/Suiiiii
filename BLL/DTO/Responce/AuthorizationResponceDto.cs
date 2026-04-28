using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Responce
{
    public class AuthorizationResponceDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public DateTime? Expires { get; set; }
        public string? Role { get; set; }
        public string? UserName { get; set; }
        public int CitizenId { get; set; }
    }
}
