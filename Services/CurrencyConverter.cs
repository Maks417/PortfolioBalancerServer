using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PortfolioBalancerServer.Domain;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Services;

public class CurrencyConverter : ICurrencyConverter
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyConverter> _logger;

    public CurrencyConverter(HttpClient httpClient, IMemoryCache cache, ILogger<CurrencyConverter> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount)> Convert(
        IEnumerable<Asset> stocks,
        IEnumerable<Asset> bonds,
        Asset contribution)
    {
        var (usd, eur) = await GetRatesInRubAsync();

        if (usd == null || eur == null)
        {
            _logger.LogWarning("USD or EUR rate missing from cache or provider response.");
            throw new ExchangeRatesUnavailableException();
        }

        var stocksAmount = stocks.Sum(x => ConvertToRub(x, usd, eur));
        var bondsAmount = bonds.Sum(x => ConvertToRub(x, usd, eur));
        var contributionAmount = ConvertToRub(contribution, usd, eur);

        var resultCurrency = SupportedCurrency.Normalize(contribution.Currency);
        var convertedStocksAmount = ConvertFromRub(resultCurrency, stocksAmount, usd, eur);
        var convertedBondsAmount = ConvertFromRub(resultCurrency, bondsAmount, usd, eur);
        var convertedContributionAmount = ConvertFromRub(resultCurrency, contributionAmount, usd, eur);

        return (convertedStocksAmount, convertedBondsAmount, convertedContributionAmount);
    }

    public async Task<bool> AreRatesAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (usd, eur) = await GetRatesInRubAsync(cancellationToken);
            return usd != null && eur != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exchange rate readiness check failed.");
            return false;
        }
    }

    private async Task<(Currency? usd, Currency? eur)> GetRatesInRubAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue("usd", out Currency? usd) && _cache.TryGetValue("eur", out Currency? eur))
        {
            return (usd, eur);
        }

        try
        {
            var responseString = await _httpClient.GetStringAsync("daily_json.js", cancellationToken);
            var rate = JsonSerializer.Deserialize<ExchangeRate>(responseString);

            if (rate?.Currency == null
                || !rate.Currency.TryGetValue("USD", out usd)
                || !rate.Currency.TryGetValue("EUR", out eur))
            {
                return (null, null);
            }

            _cache.Set("usd", usd, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
            _cache.Set("eur", eur, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

            return (usd, eur);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch exchange rates.");
            throw new ExchangeRatesUnavailableException("Failed to fetch exchange rates.", ex);
        }
    }

    private static decimal ConvertToRub(Asset asset, Currency usd, Currency eur)
    {
        return SupportedCurrency.Normalize(asset.Currency) switch
        {
            "usd" => asset.Value * RatePerUnit(usd),
            "eur" => asset.Value * RatePerUnit(eur),
            _ => asset.Value
        };
    }

    private static decimal ConvertFromRub(string resultCurrency, decimal value, Currency usd, Currency eur)
    {
        return resultCurrency switch
        {
            "usd" => value / RatePerUnit(usd),
            "eur" => value / RatePerUnit(eur),
            _ => value
        };
    }

    private static decimal RatePerUnit(Currency currency) =>
        currency.Value / currency.Nominal;
}
