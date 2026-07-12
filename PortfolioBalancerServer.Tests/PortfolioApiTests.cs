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
    public async Task HealthReady_ReturnsOk_WhenRatesAvailable()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Rates_ReturnsRatesPayload()
    {
        var response = await _client.GetAsync("/api/portfolio/rates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("test", body.GetProperty("source").GetString());
        Assert.True(body.GetProperty("ratesPerUnitInRub").TryGetProperty("usd", out _));
    }

    [Fact]
    public async Task Calculate_MatchesGoldenContractFixture()
    {
        var requestPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Contracts", "calculate-request.golden.json");
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Contracts", "calculate-response.golden.json");
        var requestJson = await File.ReadAllTextAsync(requestPath);
        var expectedJson = await File.ReadAllTextAsync(expectedPath);
        var expected = JsonSerializer.Deserialize<JsonElement>(expectedJson);

        using var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/portfolio/calculate", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected.GetProperty("stocksDiff").GetDecimal(), body.GetProperty("stocksDiff").GetDecimal());
        Assert.Equal(expected.GetProperty("bondsDiff").GetDecimal(), body.GetProperty("bondsDiff").GetDecimal());
        Assert.Equal(expected.GetProperty("currency").GetString(), body.GetProperty("currency").GetString());
        Assert.Equal(expected.GetProperty("mode").GetString(), body.GetProperty("mode").GetString());
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
        Assert.True(body.TryGetProperty("stocksBreakdown", out _));
        Assert.True(body.TryGetProperty("fx", out _));
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
    public async Task Calculate_WithRebalanceMode_AllowsNegativeDiffs()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/calculate", new
        {
            ratio = "50/50",
            stockValues = new[] { new { value = 80m, currency = "rub" } },
            bondValues = new[] { new { value = 20m, currency = "rub" } },
            contributionAmount = new { value = 0m, currency = "rub" },
            mode = "rebalance"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("stocksDiff").GetDecimal() < 0);
        Assert.True(body.GetProperty("bondsDiff").GetDecimal() > 0);
        Assert.Equal("rebalance", body.GetProperty("mode").GetString());
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
