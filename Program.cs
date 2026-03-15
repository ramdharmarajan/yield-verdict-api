using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Text;
using YieldverdictApi.Middleware;
using YieldverdictApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Read config from environment variables (Railway) or appsettings
var config = builder.Configuration;
var anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? config["Anthropic:ApiKey"] ?? string.Empty;
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? config["Supabase:Url"] ?? string.Empty;
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") ?? config["Supabase:ServiceKey"] ?? string.Empty;
var stripeSecret = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? config["Stripe:SecretKey"] ?? string.Empty;
var stripeWebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? config["Stripe:WebhookSecret"] ?? string.Empty;
var allowedOrigin = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN") ?? config["AllowedOrigin"] ?? "http://localhost:3000";
var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

// Override config with env vars
if (!string.IsNullOrEmpty(anthropicKey)) builder.Configuration["Anthropic:ApiKey"] = anthropicKey;
if (!string.IsNullOrEmpty(supabaseUrl)) builder.Configuration["Supabase:Url"] = supabaseUrl;
if (!string.IsNullOrEmpty(supabaseKey)) builder.Configuration["Supabase:ServiceKey"] = supabaseKey;
if (!string.IsNullOrEmpty(stripeSecret)) builder.Configuration["Stripe:SecretKey"] = stripeSecret;
if (!string.IsNullOrEmpty(stripeWebhookSecret)) builder.Configuration["Stripe:WebhookSecret"] = stripeWebhookSecret;

// Stripe
StripeConfiguration.ApiKey = stripeSecret;

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// HttpClient registrations
builder.Services.AddHttpClient("Postcodes");
builder.Services.AddHttpClient("LandRegistry");
builder.Services.AddHttpClient("Police");
builder.Services.AddHttpClient("FloodRisk");
builder.Services.AddHttpClient("Anthropic");
builder.Services.AddHttpClient("Supabase");

// Services
builder.Services.AddScoped<IPostcodeService, PostcodeService>();
builder.Services.AddScoped<ILandRegistryService, LandRegistryService>();
builder.Services.AddScoped<IPoliceService, PoliceService>();
builder.Services.AddScoped<IFloodRiskService, FloodRiskService>();
builder.Services.AddScoped<IInsightsService, InsightsService>();
builder.Services.AddScoped<CalculationService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = !string.IsNullOrEmpty(supabaseKey),
            IssuerSigningKey = string.IsNullOrEmpty(supabaseKey)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure port
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<SupabaseAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();
