using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace YieldverdictApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostcodesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PostcodesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<string>());

        try
        {
            var client = _httpClientFactory.CreateClient("Postcodes");
            var encoded = Uri.EscapeDataString(q);
            var response = await client.GetAsync($"https://api.postcodes.io/postcodes/{encoded}/autocomplete");

            if (!response.IsSuccessStatusCode)
                return Ok(Array.Empty<string>());

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("result", out var resultEl) ||
                resultEl.ValueKind == JsonValueKind.Null)
                return Ok(Array.Empty<string>());

            var suggestions = resultEl.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => s != null)
                .Cast<string>()
                .ToArray();

            return Ok(suggestions);
        }
        catch
        {
            return Ok(Array.Empty<string>());
        }
    }
}
