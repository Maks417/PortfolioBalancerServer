namespace PortfolioBalancerServer.Models;

public record PositionAllocation
{
    public decimal Amount { get; init; }

    public required string Currency { get; init; }

    public bool IsSell { get; init; }
}
