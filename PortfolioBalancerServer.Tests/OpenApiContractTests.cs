using System.Net.Http.Json;
using System.Text.Json;

namespace PortfolioBalancerServer.Tests;

public class OpenApiContractTests : IClassFixture<PortfolioApiFactory>
{
    private readonly HttpClient _client;

    public OpenApiContractTests(PortfolioApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Calculate_ResponseIncludesExtendedContractFields()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "60/30/10",
            stockValues = new[] { new { value = 60m, currency = "rub" } },
            bondValues = new[] { new { value = 30m, currency = "rub" } },
            cashValues = new[] { new { value = 10m, currency = "rub" } },
            contributionAmount = new { value = 0m, currency = "rub" },
            mode = "rebalance",
            driftThreshold = 0m,
            minTradeAmount = 0m
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("cashDiff", out _));
        Assert.True(body.TryGetProperty("fx", out _));
        Assert.True(body.TryGetProperty("stocksBreakdown", out _));
    }

    [Fact]
    public async Task Calculate_WithDriftThreshold_SkipsRebalanceWithinBand()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "50/50",
            stockValues = new[] { new { value = 51m, currency = "rub" } },
            bondValues = new[] { new { value = 49m, currency = "rub" } },
            contributionAmount = new { value = 0m, currency = "rub" },
            mode = "rebalance",
            driftThreshold = 5m
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("toleranceNote", out var note));
        Assert.False(string.IsNullOrWhiteSpace(note.GetString()));
    }
}
