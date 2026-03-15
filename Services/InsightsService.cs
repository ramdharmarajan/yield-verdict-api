using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using YieldverdictApi.Models.Responses;

namespace YieldverdictApi.Services;

public class InsightsService : IInsightsService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public InsightsService(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient("Anthropic");
        _config = config;
    }

    public async Task<List<InsightResponse>> GenerateInsightsAsync(AnalysisResponse analysis)
    {
        var apiKey = _config["Anthropic:ApiKey"] ?? string.Empty;

        var systemPrompt = """
            You are a UK property investment analyst.
            Analyse this data and return exactly 4 insights as a JSON array.
            Each insight has: type (opportunity/warning/strategy/market),
            title (max 5 words), insight (2-3 sentences, specific and actionable),
            confidence (High/Medium/Low).
            Return only valid JSON, no markdown.
            """;

        var userPrompt = $"""
            Postcode: {analysis.Postcode}, Area: {analysis.Area}, Region: {analysis.Region}
            Avg Price: £{analysis.AvgPrice:N0}, Price Growth 1y: {analysis.PriceGrowth1y}%, 5y: {analysis.PriceGrowth5y}%, 10y: {analysis.PriceGrowth10y}%
            Avg Rent: £{analysis.AvgRent:N0}/mo, Gross Yield: {analysis.GrossYield}%, Net Yield: {analysis.NetYield}%
            Monthly Cashflow: £{analysis.MonthlyCashflow:N0}, Annual Cashflow: £{analysis.AnnualCashflow:N0}
            Crime Index: {analysis.CrimeIndex}, Flood Risk: {analysis.FloodRisk}
            Stamp Duty: £{analysis.StampDuty:N0}, Deposit: £{analysis.Deposit:N0}, Total Upfront: £{analysis.TotalUpfront:N0}
            Monthly Mortgage: £{analysis.MonthlyMortgage:N0}, Section 24 Tax: £{analysis.Section24Tax:N0}
            Post-Tax Cashflow: £{analysis.PostTaxCashflow:N0}, Grade: {analysis.Grade}
            LTV: {analysis.Scenario.Ltv}%, Rate: {analysis.Scenario.InterestRate}%, Mortgage: {analysis.Scenario.MortgageType}
            """;

        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "[]";

        return JsonSerializer.Deserialize<List<InsightResponse>>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }
}
