using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using PortfolioBalancerServer.Endpoints;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Options;
using PortfolioBalancerServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<CurrencyServiceOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors();

builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "PortfolioBalancerServer", Version = "v1" }); });

builder.Services.AddSingleton<ICalculationService, CalculationService>();

builder.Services.AddHttpClient<ICurrencyConverter, CurrencyConverter>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CurrencyServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.CurrencyServiceUrl);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

app.UseSwagger();

app.UseHttpsRedirection();

app.UseCors(b => b
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());

app.MapPortfolioEndpoints();

app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PortfolioBalancerServer v1"));

app.Run();
