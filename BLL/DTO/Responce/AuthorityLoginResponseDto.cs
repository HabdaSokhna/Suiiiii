using BLL.DTO.Authority;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Responce
{
    public class AuthorityLoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public IEnumerable<AuthorityReportResponceDto> InitialReports { get; set; } = new List<AuthorityReportResponceDto>();
    }
}
