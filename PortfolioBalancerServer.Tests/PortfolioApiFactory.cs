using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Tests;

public sealed class PortfolioApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("EnableSwagger", "false");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICurrencyConverter>();
            services.AddSingleton<ICurrencyConverter, FakeCurrencyConverter>();
        });
    }

    private sealed class FakeCurrencyConverter : ICurrencyConverter
    {
        private static readonly FxMetadata Fx = new()
        {
            Source = "test",
            RatesAsOf = new DateTime(2026, 1, 1),
            FromCache = false,
            Stale = false,
            RatesPerUnitInRub = new Dictionary<string, decimal>
            {
                ["rub"] = 1m,
                ["usd"] = 90m,
                ["eur"] = 100m
            }
        };

        public Task<ConversionResult> ConvertAsync(
            IEnumerable<Asset> stocks,
            IEnumerable<Asset> bonds,
            Asset contribution,
            CancellationToken cancellationToken = default)
        {
            var stocksAmount = stocks.Sum(asset => asset.Value);
            var bondsAmount = bonds.Sum(asset => asset.Value);
            return Task.FromResult(new ConversionResult(stocksAmount, bondsAmount, contribution.Value, Fx));
        }

        public Task<RatesResponse> GetRatesResponseAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RatesResponse
            {
                Source = "test",
                RatesAsOf = Fx.RatesAsOf,
                FromCache = false,
                Stale = false,
                RatesPerUnitInRub = Fx.RatesPerUnitInRub
            });

        public Task<bool> AreRatesAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
