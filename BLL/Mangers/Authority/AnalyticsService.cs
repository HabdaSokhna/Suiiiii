using BLL.DTO.Authority;
using Database;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BLL.Mangers.Authority
{
    public interface IAnalyticsService
    {
        // إضافة الـ authId كباراميتر إلزامي للفلترة
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
                .Select(h => new
                {
                    h.Report.Report_ID,
                    h.Report.Report_Category,
                    h.Report.Report_Submit,
                    h.Report.Report_GeoLocation,
                    h.Status
                })
                .ToListAsync();

            // ✅ معالجة كل report وتحويل الـ Coordinates لعنوان
            var mapData = new List<MapReportDto>();

            foreach (var r in reports)
            {
                var coords = r.Report_GeoLocation.Split(',');
                double lat = 0, lng = 0;

                if (coords.Length >= 2)
                {
                    double.TryParse(coords[0].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out lat);
                    double.TryParse(coords[1].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out lng);
                }

                // ✅ تحويل الـ Coordinates لعنوان حقيقي
                var address = await _geocoding.GetAddressAsync(r.Report_GeoLocation);

                // ✅ Delay عشان Nominatim ميبلوكش
                await Task.Delay(1000);

                mapData.Add(new MapReportDto
                {
                    Id = r.Report_ID,
                    Category = r.Report_Category ?? "General",
                    Date = r.Report_Submit,
                    Status = r.Status,
                    Latitude = lat,
                    Longitude = lng,
                    Location = address  
                });
            }

            return mapData;
        }
    }
}