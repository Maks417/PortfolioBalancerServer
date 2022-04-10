using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using MiniValidation;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors();

builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "PortfolioBalancerServer", Version = "v1" }); });

builder.Services.AddTransient<ICurrencyConverter, CurrencyConverter>();
builder.Services.AddSingleton<ICalculationService, CalculationService>();

builder.Services.AddHttpClient<ICurrencyConverter, CurrencyConverter>(client => { client.BaseAddress = new Uri(builder.Configuration["CurrencyServiceUrl"]); });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();

app.UseHttpsRedirection();

app.UseCors(b => b
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());

app.MapPost("api/portfolio/calculate", async (ICurrencyConverter currencyConverter, ICalculationService calculationService, [FromBody]CalculationData formData) =>
{
    if (!MiniValidator.TryValidate(formData, out var errors))
    {
        return Results.BadRequest(errors.Values);
    }

    var (stocksAmount, bondsAmount, contributionAmount) = await currencyConverter.Convert(formData.StockValues, formData.BondValues, formData.ContributionAmount);

    var (firstRatio, secondRatio) = calculationService.ParseRatio(formData.Ratio);
    if (firstRatio == decimal.Zero)
    {
        return Results.BadRequest(new[] { new[] { "Ratio must have 100 in sum for format like '70/30'." } });
    }

    var assetsDiff = calculationService.SplitAssetsByRatio(stocksAmount, bondsAmount, contributionAmount, firstRatio, secondRatio);
    assetsDiff.Currency = formData.ContributionAmount.Currency;

    return Results.Ok(assetsDiff);
});

app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PortfolioBalancerServer v1"));

app.Run();