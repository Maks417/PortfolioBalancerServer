namespace PortfolioBalancerServer.Models;

public record ConversionResult(
    decimal StocksAmount,
    decimal BondsAmount,
    decimal CashAmount,
    decimal ContributionAmount,
    FxMetadata Fx);
