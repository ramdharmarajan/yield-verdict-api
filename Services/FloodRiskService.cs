using System.Text.Json;

namespace YieldverdictApi.Services;

public class FloodRiskService : IFloodRiskService
{
    private readonly HttpClient _http;

    public FloodRiskService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("FloodRisk");
    }

    public async Task<string> GetFloodRiskAsync(decimal lat, decimal lng)
    {
        try
        {
            var url = $"https://environment.data.gov.uk/flood-monitoring/id/floodAreas?lat={lat}&long={lng}&dist=2";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return "Low";

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("items", out var items))
                return "Low";

            var count = items.GetArrayLength();
            return count switch
            {
                0 => "Low",
                <= 2 => "Medium",
                _ => "High"
            };
        }
        catch
        {
            return "Low";
        }
    }
}
