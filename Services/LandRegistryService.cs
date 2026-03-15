using System.Text.Json;

namespace YieldverdictApi.Services;

public class LandRegistryService : ILandRegistryService
{
    private readonly HttpClient _http;

    public LandRegistryService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("LandRegistry");
    }

    public async Task<PriceData> GetPriceDataAsync(string postcode)
    {
        try
        {
            var encoded = Uri.EscapeDataString(postcode);
            var url = $"https://landregistry.data.gov.uk/data/ppi/transaction-record.json" +
                      $"?propertyAddress.postcode={encoded}&_pageSize=100";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return DefaultPriceData();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("result", out var resultEl) ||
                !resultEl.TryGetProperty("items", out var itemsEl))
                return DefaultPriceData();

            var transactions = new List<(decimal price, DateTime date)>();
            foreach (var item in itemsEl.EnumerateArray())
            {
                if (item.TryGetProperty("pricePaid", out var priceEl) &&
                    item.TryGetProperty("transactionDate", out var dateEl))
                {
                    var price = priceEl.GetDecimal();
                    if (DateTime.TryParse(dateEl.GetString(), out var date))
                        transactions.Add((price, date));
                }
            }

            if (transactions.Count == 0)
                return DefaultPriceData();

            var now = DateTime.UtcNow;
            var avgPrice = transactions.Average(t => t.price);

            var last12m = transactions.Where(t => t.date >= now.AddMonths(-12)).ToList();
            var prev12m = transactions.Where(t => t.date >= now.AddMonths(-24) && t.date < now.AddMonths(-12)).ToList();
            var last5y = transactions.Where(t => t.date >= now.AddYears(-5) && t.date < now.AddYears(-4)).ToList();
            var last10y = transactions.Where(t => t.date >= now.AddYears(-10) && t.date < now.AddYears(-9)).ToList();

            decimal growth1y = CalcGrowth(last12m, prev12m, 3m);
            decimal growth5y = CalcGrowth(last12m, last5y, 15m);
            decimal growth10y = CalcGrowth(last12m, last10y, 50m);

            return new PriceData
            {
                AvgPrice = Math.Round(avgPrice, 0),
                PriceGrowth1y = growth1y,
                PriceGrowth5y = growth5y,
                PriceGrowth10y = growth10y,
            };
        }
        catch
        {
            return DefaultPriceData();
        }
    }

    private static decimal CalcGrowth(List<(decimal price, DateTime date)> recent,
        List<(decimal price, DateTime date)> old, decimal fallback)
    {
        if (recent.Count == 0 || old.Count == 0) return fallback;
        var recentAvg = recent.Average(t => t.price);
        var oldAvg = old.Average(t => t.price);
        if (oldAvg == 0) return fallback;
        return Math.Round((recentAvg - oldAvg) / oldAvg * 100, 1);
    }

    private static PriceData DefaultPriceData() => new()
    {
        AvgPrice = 350000,
        PriceGrowth1y = 3,
        PriceGrowth5y = 15,
        PriceGrowth10y = 50,
    };
}
