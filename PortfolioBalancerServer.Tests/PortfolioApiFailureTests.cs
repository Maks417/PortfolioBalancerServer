using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Services;

namespace PortfolioBalancerServer.Tests;

public class PortfolioApiFailureTests : IClassFixture<UnavailableRatesApiFactory>
{
    private readonly HttpClient _client;

    public PortfolioApiFailureTests(UnavailableRatesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Calculate_WhenRatesUnavailable_Returns503()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "50/50",
            stockValues = new[] { new { value = 60m, currency = "rub" } },
            bondValues = new[] { new { value = 40m, currency = "rub" } },
            contributionAmount = new { value = 100m, currency = "rub" }
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Rates_WhenRatesUnavailable_Returns503()
    {
        var response = await _client.GetAsync("/api/portfolio/rates");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_WhenRatesUnavailable_ReturnsUnhealthy()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}

public sealed class UnavailableRatesApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("EnableSwagger", "false");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICurrencyConverter>();
            services.AddSingleton<ICurrencyConverter, UnavailableCurrencyConverter>();
        });
    }

    private sealed class UnavailableCurrencyConverter : ICurrencyConverter
    {
        public Task<ConversionResult> ConvertAsync(
            IEnumerable<Asset> stocks,
            IEnumerable<Asset> bonds,
            Asset contribution,
            CancellationToken cancellationToken = default) =>
            throw new ExchangeRatesUnavailableException();

        public Task<RatesResponse> GetRatesResponseAsync(CancellationToken cancellationToken = default) =>
            throw new ExchangeRatesUnavailableException();

        public Task<bool> AreRatesAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
