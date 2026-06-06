using BLL.DTO.Authority;
using Database;
using Microsoft.EntityFrameworkCore;

namespace BLL.Mangers.Authority
{
    public interface IAnalyticsService
    {
        Task<IEnumerable<MapReportDto>> GetReportsMapDataAsync(int authId);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly Ai_Reports_Context _context;
        private readonly IGeocodingService _geocoding;

        public AnalyticsService(Ai_Reports_Context context, IGeocodingService geocoding)
        {
            _context = context;
            _geocoding = geocoding;
        }

        public async Task<IEnumerable<MapReportDto>> GetReportsMapDataAsync(int authId)
        {
            var reports = await _context.TbHandle
                .Where(h => h.Authority_ID == authId &&
                            !h.Report.IsDeleted &&
                            !string.IsNullOrEmpty(h.Report.Report_GeoLocation))
                .GroupBy(h => h.Report_ID)
                .Select(g => new
                {
                    g.First().Report.Report_ID,
                    g.First().Report.Report_Category,
                    g.First().Report.Report_Submit,
                    g.First().Report.Report_GeoLocation,
                    g.First().Report.Report_Description,
                    Status = g.OrderByDescending(h => h.Handle_ID)
                               .Select(h => h.Status)
                               .FirstOrDefault() ?? "Pending"
                })
                .ToListAsync();

            var mapData = new List<MapReportDto>();

            foreach (var r in reports)
            {
                // ✅ Parse Coordinates
                var coords = r.Report_GeoLocation.Split(',');
                double lat = 0, lng = 0;

                if (coords.Length >= 2)
                {
                    double.TryParse(coords[0].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out lat);
                    double.TryParse(coords[1].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out lng);
                }

                // ✅ Parse Title & Description نفس طريقة GetReportDetails
                string fullDescription = r.Report_Description ?? "";
                string title = "بدون عنوان";
                string descriptionBody = fullDescription;

                if (fullDescription.Contains("Title:") && fullDescription.Contains("Description:"))
                {
                    var parts = fullDescription
                        .Replace("Title:", "")
                        .Split(new[] { "Description:" }, StringSplitOptions.None);

                    if (parts.Length == 2)
                    {
                        title = parts[0].Trim();
                        descriptionBody = parts[1].Trim();
                    }
                }

                // ✅ Get Address
                var address = await _geocoding.GetAddressAsync(r.Report_GeoLocation);

                mapData.Add(new MapReportDto
                {
                    Id = r.Report_ID,
                    Category = r.Report_Category ?? "General",
                    Date = r.Report_Submit,
                    Status = r.Status,
                    Latitude = lat,
                    Longitude = lng,
                    Location = address,
                    Title = title,
                    Description = descriptionBody
                });
            }

            return mapData;
        }
    }
}