using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioBalancerServer.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private const decimal usdToRub = 74;
        private const decimal eurToRub = 83;

        public (decimal stocksAmount, decimal bondsAmount, decimal contibutionAmount) ConvertToRub(
            IEnumerable<Asset> stocks, IEnumerable<Asset> bonds, Asset contributionAmount)
        {
            var stocksAmount = stocks.Sum(x => SumAsset(x));
            var bondsAmount = bonds.Sum(x => SumAsset(x));

            return (stocksAmount, bondsAmount, SumAsset(contributionAmount));
        }

        private static decimal SumAsset(Asset asset)
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
    }
}
