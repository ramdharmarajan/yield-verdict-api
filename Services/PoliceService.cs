using System.Text.Json;

namespace YieldverdictApi.Services;

public class PoliceService : IPoliceService
{
    private readonly HttpClient _http;

    public PoliceService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Police");
    }

    public async Task<int> GetCrimeIndexAsync(decimal lat, decimal lng)
    {
        try
        {
            var date = DateTime.UtcNow.ToString("yyyy-MM");
            var count = await FetchCrimeCount(lat, lng, date);

            if (count < 0)
            {
                // Try previous month
                date = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM");
                count = await FetchCrimeCount(lat, lng, date);
            }

            if (count < 0) return 40;

            return count switch
            {
                <= 10 => 20,
                <= 25 => 35,
                <= 50 => 50,
                <= 80 => 65,
                <= 120 => 75,
                _ => 85
            };
        }
        catch
        {
            return 40;
        }
    }

    private async Task<int> FetchCrimeCount(decimal lat, decimal lng, string date)
    {
        var url = $"https://data.police.uk/api/crimes-at-location?lat={lat}&lng={lng}&date={date}";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return -1;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return doc.RootElement.GetArrayLength();

        return -1;
    }
}
