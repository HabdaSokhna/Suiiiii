
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public interface IGeocodingService
{
    Task<string> GetAddressAsync(string geoLocation);
}

public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    public GeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SIRS-App");
        
    }

    public async Task<string> GetAddressAsync(string geoLocation)
    {
        try
        {
            // الـ geoLocation مخزنة كده "30.0444,31.2357"
            var parts = geoLocation.Split(',');
            if (parts.Length != 2) return geoLocation;

            var lat = parts[0].Trim();
            var lon = parts[1].Trim();

            var url = $"https://nominatim.openstreetmap.org/reverse?lat={lat}&lon={lon}&format=json&accept-language=ar";

            var response = await _httpClient.GetFromJsonAsync<NominatimResponse>(url);

            return response?.DisplayName ?? geoLocation;
        }
        catch
        {
            return geoLocation;
        }
    }
}

// الـ Model بتاع الـ Response
public class NominatimResponse
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }
}