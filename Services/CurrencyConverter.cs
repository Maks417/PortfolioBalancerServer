using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PortfolioBalancerServer.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private readonly HttpClient _httpClient;

        public CurrencyConverter(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(decimal stocksAmount, decimal bondsAmount, decimal contibutionAmount)> Convert(
            IEnumerable<Asset> stocks, IEnumerable<Asset> bonds, Asset contribution)
        {
            var responseString = await _httpClient.GetStringAsync("daily_json.js");
            var course = JsonSerializer.Deserialize<ExchangeCourse>(responseString);

            if (!course.Currency.TryGetValue("USD", out var usd) 
                || !course.Currency.TryGetValue("EUR", out var eur))
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
