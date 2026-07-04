using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Services;

namespace PortfolioBalancerServer.Tests;

public class CurrencyConverterTests
{
    [Fact]
    public async Task Convert_UsesNominalWhenConvertingUsdToRub()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "Valute": {
                "USD": { "Nominal": 10, "Value": 750 },
                "EUR": { "Nominal": 1, "Value": 90 }
              }
            }
            """);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        var sut = new CurrencyConverter(httpClient, new MemoryCache(new MemoryCacheOptions()), NullLogger<CurrencyConverter>.Instance);

        var (stocks, _, _) = await sut.Convert(
            [new Asset { Value = 10, Currency = "usd" }],
            [],
            new Asset { Value = 0, Currency = "rub" });

        Assert.Equal(750, stocks);
    }

    [Fact]
    public async Task Convert_NormalizesCurrencyCase()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "Valute": {
                "USD": { "Nominal": 1, "Value": 100 },
                "EUR": { "Nominal": 1, "Value": 110 }
              }
            }
            """);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        var sut = new CurrencyConverter(httpClient, new MemoryCache(new MemoryCacheOptions()), NullLogger<CurrencyConverter>.Instance);

        var (stocks, _, _) = await sut.Convert(
            [new Asset { Value = 1, Currency = "USD" }],
            [],
            new Asset { Value = 0, Currency = "RUB" });

        Assert.Equal(100, stocks);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _json;

        public StubHttpMessageHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json)
            });
    }
}
