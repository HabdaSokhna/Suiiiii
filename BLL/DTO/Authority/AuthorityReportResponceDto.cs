using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Authority
{
    public class AuthorityReportResponceDto
    {
        public int ReportId { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string? PhotoPath { get; set; }
        public string Status { get; set; } // Pending, Accepted, etc.
        public string AICategory { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
