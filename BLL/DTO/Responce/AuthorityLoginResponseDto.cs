using BLL.DTO.Authority;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Responce
{
    public class AuthorityLoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public IEnumerable<AuthorityReportResponceDto> InitialReports { get; set; } = new List<AuthorityReportResponceDto>();
    }
}
