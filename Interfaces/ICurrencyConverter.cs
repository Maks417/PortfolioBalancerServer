using PortfolioBalancerServer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortfolioBalancerServer.Interfaces
{
    public interface ICurrencyConverter
    {
        Task<(decimal stocksAmount, decimal bondsAmount, decimal contibutionAmount)> Convert(IEnumerable<Asset> stocks, IEnumerable<Asset> bonds, Asset contributionAmount);
    }
}
