namespace BLL.DTO.Authority
{
    public class IncidentVolumeDto
    {
        public string Day { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int SolvedCount { get; set; }
        public int Total { get; set; }
    }
}
