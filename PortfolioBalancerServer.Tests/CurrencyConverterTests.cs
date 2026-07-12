using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Services;

namespace PortfolioBalancerServer.Tests;

public class CurrencyConverterTests
{
    [Fact]
    public async Task ConvertAsync_UsesRatesFromProvider()
    {
        var provider = new StubRateProvider(
            new Currency { Nominal = 10, Value = 750 },
            new Currency { Nominal = 1, Value = 90 });
        var sut = new CurrencyConverter(provider);

        var result = await sut.ConvertAsync(
            [new Asset { Value = 10, Currency = "usd" }],
            [],
            [],
            new Asset { Value = 0, Currency = "rub" });

        Assert.Equal(750, result.StocksAmount);
        Assert.Equal("stub", result.Fx.Source);
        Assert.Equal(75, result.Fx.RatesPerUnitInRub["usd"]);
    }

    [Fact]
    public async Task ConvertAsync_NormalizesCurrencyCase()
    {
        var provider = new StubRateProvider(
            new Currency { Nominal = 1, Value = 100 },
            new Currency { Nominal = 1, Value = 110 });
        var sut = new CurrencyConverter(provider);

        var result = await sut.ConvertAsync(
            [new Asset { Value = 1, Currency = "USD" }],
            [],
            [],
            new Asset { Value = 0, Currency = "RUB" });

        Assert.Equal(100, result.StocksAmount);
    }

    [Fact]
    public async Task GetRatesResponseAsync_ReturnsProviderSnapshot()
    {
        var provider = new StubRateProvider(
            new Currency { Nominal = 1, Value = 90 },
            new Currency { Nominal = 1, Value = 100 },
            new DateTime(2026, 3, 1));
        var sut = new CurrencyConverter(provider);

        var rates = await sut.GetRatesResponseAsync();

        Assert.Equal("stub", rates.Source);
        Assert.Equal(90, rates.RatesPerUnitInRub["usd"]);
        Assert.True(rates.Stale);
    }

    private sealed class StubRateProvider : IRateProvider
    {
        private readonly RateSnapshot _snapshot;

        public StubRateProvider(Currency usd, Currency eur, DateTime? asOf = null, bool stale = true)
        {
            _snapshot = new RateSnapshot(usd, eur, asOf, false, stale, "stub");
        }

        public Task<RateSnapshot> GetRatesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
