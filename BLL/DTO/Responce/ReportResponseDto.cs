using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Responce
{
    public class ReportResponseDto
    {
        public bool IsSuccess { get; set; } = true;

       
        public string Message { get; set; } = string.Empty;

        
        public int ReportId { get; set; }

        
        public string FinalCategory { get; set; } = string.Empty;

        
        public string FormattedConfidence { get; set; } = "0%";

        
        public DateTime SubmittedAt { get; set; }

        
        public string InitialStatus { get; set; } = "Pending";
    }
}
