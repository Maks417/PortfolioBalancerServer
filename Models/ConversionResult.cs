namespace PortfolioBalancerServer.Models;

public record ConversionResult(
    decimal StocksAmount,
    decimal BondsAmount,
    decimal ContributionAmount,
    FxMetadata Fx);
