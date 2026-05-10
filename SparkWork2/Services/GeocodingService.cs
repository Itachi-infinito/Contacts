using System.Globalization;
using System.Text.Json;

namespace SparkWork2.Services;

public class GeocodingService
{
    private readonly HttpClient _httpClient = new();

    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        string url =
            "https://nominatim.openstreetmap.org/search" +
            $"?q={Uri.EscapeDataString(query.Trim())}" +
            "&format=json" +
            "&limit=1";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("SparkWork2/1.0");

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        string json = await response.Content.ReadAsStringAsync();

        var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

        var first = results?.FirstOrDefault();

        if (first == null)
            return null;

        if (!double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude))
            return null;

        if (!double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
            return null;

        return (latitude, longitude);
    }

    private class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }
}
