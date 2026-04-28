using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Responce
{
    public class RegisterResponceDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public IEnumerable<string>? Errors { get; set; }
        public int CitizenId { get; set; }
    }
}
