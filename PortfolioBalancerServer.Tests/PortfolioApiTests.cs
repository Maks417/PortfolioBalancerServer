using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PortfolioBalancerServer.Tests;

public class PortfolioApiTests : IClassFixture<PortfolioApiFactory>
{
    private readonly HttpClient _client;

    public PortfolioApiTests(PortfolioApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Calculate_WithValidRequest_ReturnsSplit()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "50/50",
            stockValues = new[] { new { value = 60m, currency = "rub" } },
            bondValues = new[] { new { value = 40m, currency = "rub" } },
            contributionAmount = new { value = 100m, currency = "rub" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(40, body.GetProperty("stocksDiff").GetDecimal());
        Assert.Equal(60, body.GetProperty("bondsDiff").GetDecimal());
        Assert.Equal("rub", body.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task Calculate_WithRatioZero_AssignsContributionToBonds()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "0",
            stockValues = new[] { new { value = 100m, currency = "rub" } },
            bondValues = new[] { new { value = 50m, currency = "rub" } },
            contributionAmount = new { value = 25m, currency = "rub" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("stocksDiff").GetDecimal());
        Assert.Equal(25, body.GetProperty("bondsDiff").GetDecimal());
    }

    [Fact]
    public async Task Calculate_WithEmptyPositionRows_IgnoresEmptyValues()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "100",
            stockValues = new object[]
            {
                new { value = "", currency = "rub" },
                new { value = 100m, currency = "rub" }
            },
            bondValues = new object[] { new { value = "", currency = "rub" } },
            contributionAmount = new { value = 10m, currency = "rub" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Calculate_WithInvalidRatio_ReturnsValidationProblemWithFieldKeys()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "70/40",
            stockValues = new[] { new { value = 100m, currency = "rub" } },
            bondValues = new[] { new { value = 50m, currency = "rub" } },
            contributionAmount = new { value = 25m, currency = "rub" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("ratio", out _));
    }

    [Fact]
    public async Task Calculate_WithUnsupportedCurrency_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "50/50",
            stockValues = new[] { new { value = 100m, currency = "gbp" } },
            bondValues = new[] { new { value = 50m, currency = "rub" } },
            contributionAmount = new { value = 25m, currency = "rub" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any());
    }
}
