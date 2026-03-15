using System.Text.Json;
using YieldverdictApi.Models.Responses;

namespace YieldverdictApi.Services;

public class PostcodeService : IPostcodeService
{
    private readonly HttpClient _http;

    public PostcodeService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Postcodes");
    }

    public async Task<PostcodeResult?> ValidateAsync(string postcode)
    {
        try
        {
            var encoded = Uri.EscapeDataString(postcode);
            var response = await _http.GetAsync($"https://api.postcodes.io/postcodes/{encoded}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("result");

            return new PostcodeResult
            {
                IsValid = true,
                Postcode = result.GetProperty("postcode").GetString() ?? postcode,
                Latitude = result.TryGetProperty("latitude", out var lat) ? lat.GetDecimal() : 0,
                Longitude = result.TryGetProperty("longitude", out var lng) ? lng.GetDecimal() : 0,
                District = result.TryGetProperty("parliamentary_constituency", out var dist)
                    ? dist.GetString() ?? string.Empty
                    : string.Empty,
                Region = result.TryGetProperty("region", out var reg)
                    ? reg.GetString() ?? string.Empty
                    : string.Empty,
                AdminDistrict = result.TryGetProperty("admin_district", out var admin)
                    ? admin.GetString() ?? string.Empty
                    : string.Empty,
            };
        }
        catch
        {
            return null;
        }
    }
}
