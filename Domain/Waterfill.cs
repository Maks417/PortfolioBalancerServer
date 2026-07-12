namespace PortfolioBalancerServer.Domain;

public static class Waterfill
{
    public static decimal[] DistributeEqual(IReadOnlyList<decimal> values, decimal budget)
    {
        var n = values.Count;
        if (n == 0 || budget <= 0)
        {
            return values.Select(_ => 0m).ToArray();
        }

        var sum = values.Sum();
        var target = (sum + budget) / n;
        var need = values.Select(v => Math.Max(0m, target - v)).ToArray();
        var needSum = need.Sum();
        var leftover = budget - needSum;
        var perPosition = leftover / n;

        return need.Select(v => v + perPosition).ToArray();
    }
}
