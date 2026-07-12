namespace PortfolioBalancerServer.Models;

public record FxMetadata
{
    public required string Source { get; init; }

    public DateTime? RatesAsOf { get; init; }

    public bool FromCache { get; init; }

    public bool Stale { get; init; }

    public Dictionary<string, decimal> RatesPerUnitInRub { get; init; } = new();
}
