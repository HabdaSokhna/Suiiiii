using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Responce
{
    public class ProfileResponse_Dto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
    }
}
