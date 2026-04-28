using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Authority
{
    public class SystemActionDto
    {
        public int ReportId { get; set; }
        public string Status { get; set; }
        public DateTime Time { get; set; }
        public string Category { get; set; }
        public string AI { get; set; }
    }
}
