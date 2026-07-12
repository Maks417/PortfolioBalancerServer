using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using PortfolioBalancerServer.Endpoints;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Options;
using PortfolioBalancerServer.Serialization;
using PortfolioBalancerServer.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<CurrencyServiceOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<RateOptions>(builder.Configuration.GetSection(RateOptions.SectionName));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("portfolio", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 60;
        limiter.QueueLimit = 0;
    });
});

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new FlexibleDecimalConverter());
});

builder.Services.AddMemoryCache();

var enableSwagger = builder.Configuration.GetValue("EnableSwagger", false)
    || builder.Environment.IsDevelopment();

if (enableSwagger)
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PortfolioBalancerServer",
            Version = "v1",
            Description = "API for portfolio-balancer-client. Calculates stock/bond contribution split."
        });
    });
}

builder.Services.AddSingleton<ICalculationService, CalculationService>();

builder.Services.AddHttpClient<IRateProvider, CbrRateProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CurrencyServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.CurrencyServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<ICurrencyConverter, CurrencyConverter>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck<CurrencyRatesHealthCheck>("currency_rates");

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "currency_rates"
});

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PortfolioBalancerServer v1"));
}

if (builder.Configuration.GetValue("EnableHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
if (!app.Environment.IsDevelopment()
    && (corsOptions.AllowedOrigins is not { Length: > 0 }))
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must be configured in non-development environments.");
}

app.UseCors(policy =>
{
    if (corsOptions.AllowedOrigins is { Length: > 0 })
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    }
    else
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    }
});

app.UseRateLimiter();

app.MapPortfolioEndpoints();

app.Run();

public partial class Program;
