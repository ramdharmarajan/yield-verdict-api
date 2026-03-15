using System.Text.Json;

namespace YieldverdictApi.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, error) = ex switch
        {
            ArgumentException => (400, "Bad request"),
            KeyNotFoundException => (404, "Not found"),
            UnauthorizedAccessException => (401, "Unauthorized"),
            _ => (500, "Internal server error")
        };

        context.Response.StatusCode = statusCode;

        var response = new Dictionary<string, object>
        {
            ["error"] = error,
            ["statusCode"] = statusCode
        };

        if (_env.IsDevelopment())
            response["message"] = ex.Message;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
