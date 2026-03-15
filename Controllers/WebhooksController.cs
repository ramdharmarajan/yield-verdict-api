using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace YieldverdictApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IConfiguration config, IHttpClientFactory httpClientFactory,
        ILogger<WebhooksController> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var webhookSecret = _config["Stripe:WebhookSecret"] ?? string.Empty;
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Stripe webhook signature validation failed: {Message}", ex.Message);
            return BadRequest(new { error = "Invalid signature" });
        }

        _logger.LogInformation("Stripe event received: {Type}", stripeEvent.Type);

        switch (stripeEvent.Type)
        {
            case "customer.subscription.created":
            case "customer.subscription.updated":
            {
                var sub = stripeEvent.Data.Object as Subscription;
                if (sub != null)
                    await UpsertSubscription(sub, stripeEvent.Type == "customer.subscription.created" ? "active" : null);
                break;
            }
            case "customer.subscription.deleted":
            {
                var sub = stripeEvent.Data.Object as Subscription;
                if (sub != null)
                    await UpdateSubscriptionStatus(sub.Id, "cancelled");
                break;
            }
        }

        return Ok();
    }

    private async Task UpsertSubscription(Subscription sub, string? overrideStatus)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];
        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey)) return;

        var userId = sub.Metadata.TryGetValue("userId", out var uid) ? uid : null;
        var status = overrideStatus ?? sub.Status;
        var tier = sub.Items.Data.FirstOrDefault()?.Price.Id == _config["Stripe:PortfolioPriceId"]
            ? "portfolio" : "pro";

        var currentPeriodEnd = sub.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
        var payload = new
        {
            stripe_subscription_id = sub.Id,
            stripe_customer_id = sub.CustomerId,
            user_id = userId,
            status,
            tier,
            current_period_end = currentPeriodEnd
        };

        var client = _httpClientFactory.CreateClient("Supabase");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/rest/v1/subscriptions");
        request.Headers.Add("apikey", supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");
        request.Headers.Add("Prefer", "resolution=merge-duplicates");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        await client.SendAsync(request);
    }

    private async Task UpdateSubscriptionStatus(string subscriptionId, string status)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];
        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey)) return;

        var payload = new { status };
        var client = _httpClientFactory.CreateClient("Supabase");
        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"{supabaseUrl}/rest/v1/subscriptions?stripe_subscription_id=eq.{subscriptionId}");
        request.Headers.Add("apikey", supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        await client.SendAsync(request);
    }
}
