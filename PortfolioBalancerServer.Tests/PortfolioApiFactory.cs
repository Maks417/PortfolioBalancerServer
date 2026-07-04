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
        public Task<(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount)> Convert(
            IEnumerable<Asset> stocks,
            IEnumerable<Asset> bonds,
            Asset contribution)
        {
            var stocksAmount = stocks.Sum(asset => asset.Value);
            var bondsAmount = bonds.Sum(asset => asset.Value);
            return Task.FromResult((stocksAmount, bondsAmount, contribution.Value));
        }

        public Task<bool> AreRatesAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
