using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.User
{
    public class UserStatus_Dto
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public int TotalReports { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public DateTime? LastReportDate { get; set; }
    }
}
