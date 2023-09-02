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
            var (usd, eur) = await GetRatesInRub();

            if (usd == null || eur == null)
            {
                return (decimal.Zero, decimal.Zero, decimal.Zero);
            }

            var stocksAmount = stocks.Sum(x => ConvertToRub(x, usd.Value, eur.Value));
            var bondsAmount = bonds.Sum(x => ConvertToRub(x, usd.Value, eur.Value));
            var contributionAmount = ConvertToRub(contribution, usd.Value, eur.Value);

            var convertedStocksAmount = ConvertFromRub(contribution.Currency, stocksAmount, usd.Value, eur.Value);
            var convertedBondsAmount = ConvertFromRub(contribution.Currency, bondsAmount, usd.Value, eur.Value);
            var convertedContributionAmount = ConvertFromRub(contribution.Currency, contributionAmount, usd.Value, eur.Value);

            return (convertedStocksAmount, convertedBondsAmount, convertedContributionAmount);
        }

        private async Task<(Currency usd, Currency eur)> GetRatesInRub()
        {
            if (_cache.TryGetValue("usd", out Currency usd) && _cache.TryGetValue("eur", out Currency eur))
            {
                return (usd, eur);
            }

            var responseString = await _httpClient.GetStringAsync("daily_json.js");
            var rate = JsonSerializer.Deserialize<ExchangeRate>(responseString);

            if (!rate.Currency.TryGetValue("USD", out usd)
                || !rate.Currency.TryGetValue("EUR", out eur))
            {
                return (null, null);
            }

            _cache.Set("usd", usd, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
            _cache.Set("eur", eur, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

            return (usd, eur);
        }

        private static decimal ConvertToRub(Asset asset, decimal usdToRub, decimal eurToRub)
        {
            return asset.Currency switch
            {
                "usd" => asset.Value * usdToRub,
                "eur" => asset.Value * eurToRub,
                _ => asset.Value
            };
        }

        private static decimal ConvertFromRub(string resultCurrency, decimal value, decimal usdToRub, decimal eurToRub)
        {
            return resultCurrency switch
            {
                "usd" => value / usdToRub,
                "eur" => value / eurToRub,
                _ => value
            };
        }
    }
}
