namespace PortfolioBalancerServer.Models;

public record RateSnapshot(
    Currency? Usd,
    Currency? Eur,
    DateTime? RatesAsOf,
    bool FromCache,
    bool Stale,
    string Source);
