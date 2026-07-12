using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Options;

namespace PortfolioBalancerServer.Services;

public sealed class CbrRateProvider : IRateProvider
{
    private const string SourceName = "CBR";
    private const string UsdCacheKey = "rates:usd";
    private const string EurCacheKey = "rates:eur";
    private const string UsdStaleKey = "rates:usd:stale";
    private const string EurStaleKey = "rates:eur:stale";
    private const string AsOfCacheKey = "rates:asof";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CbrRateProvider> _logger;
    private readonly TimeSpan _cacheTtl;

    public CbrRateProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<RateOptions> rateOptions,
        ILogger<CbrRateProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _cacheTtl = TimeSpan.FromHours(rateOptions.Value.CacheTtlHours);
    }

    public async Task<RateSnapshot> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(UsdCacheKey, out Currency? cachedUsd)
            && _cache.TryGetValue(EurCacheKey, out Currency? cachedEur))
        {
            return BuildSnapshot(cachedUsd, cachedEur, GetCachedAsOf(), fromCache: true, stale: false);
        }

        try
        {
            var snapshot = await FetchRatesAsync(cancellationToken);
            if (snapshot.Usd != null && snapshot.Eur != null)
            {
                CacheRates(snapshot.Usd, snapshot.Eur, snapshot.RatesAsOf);
                return snapshot with { FromCache = false, Stale = false };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Primary exchange rate fetch failed; attempting stale cache fallback.");
        }

        if (_cache.TryGetValue(UsdStaleKey, out Currency? staleUsd)
            && _cache.TryGetValue(EurStaleKey, out Currency? staleEur))
        {
            _logger.LogWarning("Serving stale exchange rates from backup cache.");
            return BuildSnapshot(staleUsd, staleEur, GetCachedAsOf(), fromCache: true, stale: true);
        }

        throw new ExchangeRatesUnavailableException();
    }

    private async Task<RateSnapshot> FetchRatesAsync(CancellationToken cancellationToken)
    {
        var responseString = await _httpClient.GetStringAsync("daily_json.js", cancellationToken);
        var rate = JsonSerializer.Deserialize<ExchangeRate>(responseString);

        if (rate?.Currency == null
            || !rate.Currency.TryGetValue("USD", out var usd)
            || !rate.Currency.TryGetValue("EUR", out var eur))
        {
            return new RateSnapshot(null, null, null, false, false, SourceName);
        }

        return BuildSnapshot(usd, eur, rate.Date, fromCache: false, stale: false);
    }

    private void CacheRates(Currency usd, Currency eur, DateTime? asOf)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheTtl
        };

        _cache.Set(UsdCacheKey, usd, cacheOptions);
        _cache.Set(EurCacheKey, eur, cacheOptions);
        _cache.Set(UsdStaleKey, usd);
        _cache.Set(EurStaleKey, eur);
        if (asOf.HasValue)
        {
            _cache.Set(AsOfCacheKey, asOf.Value);
        }
    }

    private DateTime? GetCachedAsOf() =>
        _cache.TryGetValue(AsOfCacheKey, out DateTime asOf) ? asOf : null;

    private static RateSnapshot BuildSnapshot(
        Currency? usd,
        Currency? eur,
        DateTime? asOf,
        bool fromCache,
        bool stale) =>
        new(usd, eur, asOf, fromCache, stale, SourceName);
}
