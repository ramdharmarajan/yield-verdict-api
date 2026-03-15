using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YieldverdictApi.Models.Requests;
using YieldverdictApi.Services;

namespace YieldverdictApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsightsController : ControllerBase
{
    private readonly IInsightsService _insightsService;

    public InsightsController(IInsightsService insightsService)
    {
        _insightsService = insightsService;
    }

    [HttpPost]
    public async Task<IActionResult> GenerateInsights([FromBody] InsightsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = HttpContext.Items["UserId"] as string;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "Authentication required" });

        // TODO: Check subscription tier from Supabase
        // For now, allow any authenticated user
        var insights = await _insightsService.GenerateInsightsAsync(request.Analysis);
        return Ok(insights);
    }
}
