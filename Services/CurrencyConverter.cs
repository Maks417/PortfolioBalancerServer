using Microsoft.Extensions.Caching.Memory;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using System.Text.Json;

namespace PortfolioBalancerServer.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;


        public CurrencyConverter(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<(decimal stocksAmount, decimal bondsAmount, decimal contibutionAmount)> Convert(
            IEnumerable<Asset> stocks, IEnumerable<Asset> bonds, Asset contribution)
        {
            var (usd, eur) = await GetCoursesInRub();

            if (usd == null || eur == null)
            {
                return (decimal.Zero, decimal.Zero, decimal.Zero);
            }

            var stocksAmount = stocks.Sum(x => ConverToRub(x, usd.Value, eur.Value));
            var bondsAmount = bonds.Sum(x => ConverToRub(x, usd.Value, eur.Value));
            var contributionAmount = ConverToRub(contribution, usd.Value, eur.Value);

            var convertedStocksAmount = ConverFromRub(contribution.Currency, stocksAmount, usd.Value, eur.Value);
            var convertedBondsAmount = ConverFromRub(contribution.Currency, bondsAmount, usd.Value, eur.Value);
            var convertedContributionAmount = ConverFromRub(contribution.Currency, contributionAmount, usd.Value, eur.Value);

            return (convertedStocksAmount, convertedBondsAmount, convertedContributionAmount);
        }

        private async Task<(Currency usd, Currency eur)> GetCoursesInRub()
        {
            if (_cache.TryGetValue("usd", out Currency usd) && _cache.TryGetValue("eur", out Currency eur))
            {
                return (usd, eur);
            }

            var responseString = await _httpClient.GetStringAsync("daily_json.js");
            var course = JsonSerializer.Deserialize<ExchangeCourse>(responseString);

            if (!course.Currency.TryGetValue("USD", out usd)
                || !course.Currency.TryGetValue("EUR", out eur))
            {
                return (null, null);
            }

            _cache.Set("usd", usd, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
            _cache.Set("eur", eur, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

            return (usd, eur);
        }

        private static decimal ConverToRub(Asset asset, decimal usdToRub, decimal eurToRub)
        {
            switch (asset.Currency)
            {
                case "usd":
                    return asset.Value * usdToRub;
                case "eur":
                    return asset.Value * eurToRub;
                default:
                    return asset.Value;
            }
        }

        private static decimal ConverFromRub(string resultCurrency, decimal value, decimal usdToRub, decimal eurToRub)
        {
            switch (resultCurrency)
            {
                case "usd":
                    return value / usdToRub;
                case "eur":
                    return value / eurToRub;
                default:
                    return value;
            }
        }
    }
}
