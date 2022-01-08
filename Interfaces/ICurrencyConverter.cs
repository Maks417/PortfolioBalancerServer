using PortfolioBalancerServer.Models;
using System.Collections.Generic;

namespace PortfolioBalancerServer.Interfaces
{
    public interface ICurrencyConverter
    {
        (decimal stocksAmount, decimal bondsAmount, decimal contibutionAmount) ConvertToRub(IEnumerable<Asset> stocks, IEnumerable<Asset> bonds, Asset contributionAmount);
    }
}
