using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Domain;

public static class AssetFilter
{
    public static Asset[] FilterFilled(IEnumerable<Asset> assets) =>
        assets.Where(asset => asset.Value > decimal.Zero).ToArray();
}
