using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.User
{
    public class UserStatus_Dto
    {
        public string FullName { get; set; }
        public string PhotoUrl { get; set; }
        public int TotalReports { get; set; }         // ✅ كل الريبورتات
        public int CountReportsInMonth { get; set; }  // ✅ شهر الحالي بس
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
    }
}
