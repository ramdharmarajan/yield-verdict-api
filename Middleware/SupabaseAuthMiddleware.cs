using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace YieldverdictApi.Middleware;

public class SupabaseAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<SupabaseAuthMiddleware> _logger;

    public SupabaseAuthMiddleware(RequestDelegate next, IConfiguration config,
        ILogger<SupabaseAuthMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var userId = ValidateToken(token);
            if (userId != null)
            {
                context.Items["UserId"] = userId;
            }
        }

        await _next(context);
    }

    private string? ValidateToken(string token)
    {
        try
        {
            var jwtKey = _config["Supabase:JwtSecret"] ?? string.Empty;
            if (string.IsNullOrEmpty(jwtKey)) return null;

            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParams, out _);
            return principal.FindFirst("sub")?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Token validation failed: {Message}", ex.Message);
            return null;
        }
    }
}
