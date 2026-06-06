using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DTO.Authority
{
    public class MapReportDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Location { get; set; }
        public string? Description { get; set; }
        public string? Title { get; set; }
    }
}
