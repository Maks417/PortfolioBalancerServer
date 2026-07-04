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

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors();

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

builder.Services.AddHttpClient<ICurrencyConverter, CurrencyConverter>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CurrencyServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.CurrencyServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck<CurrencyRatesHealthCheck>("currency_rates");

var app = builder.Build();

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
        policy.AllowAnyMethod()
            .AllowAnyHeader()
            .AllowAnyOrigin();
    }
});

app.MapPortfolioEndpoints();

app.Run();

public partial class Program;
