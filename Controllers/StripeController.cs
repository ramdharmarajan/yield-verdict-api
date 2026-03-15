using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace YieldverdictApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StripeController : ControllerBase
{
    private readonly IConfiguration _config;

    public StripeController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
    {
        var userId = HttpContext.Items["UserId"] as string;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var priceId = request.Plan == "portfolio"
            ? _config["Stripe:PortfolioPriceId"]
            : _config["Stripe:ProPriceId"];

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                }
            ],
            Mode = "subscription",
            SuccessUrl = _config["Stripe:SuccessUrl"] ?? "http://localhost:3000/success",
            CancelUrl = _config["Stripe:CancelUrl"] ?? "http://localhost:3000/cancel",
            Metadata = new Dictionary<string, string> { ["userId"] = userId }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return Ok(new { url = session.Url });
    }

    [HttpGet("portal")]
    public async Task<IActionResult> CreatePortal([FromQuery] string customerId)
    {
        var userId = HttpContext.Items["UserId"] as string;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = _config["Stripe:PortalReturnUrl"] ?? "http://localhost:3000/account",
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);
        return Ok(new { url = session.Url });
    }
}

public class CheckoutRequest
{
    public string Plan { get; set; } = "pro";
}
