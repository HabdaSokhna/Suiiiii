namespace BLL.DTO.Authority
{
    public class ReportStatisticsDto
    {
        public int TotalReports { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
    }
}
