using PortfolioBalancerServer.Domain;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Services;

public sealed class CurrencyConverter : ICurrencyConverter
{
    private readonly IRateProvider _rateProvider;

    public CurrencyConverter(IRateProvider rateProvider)
    {
        _rateProvider = rateProvider;
    }

    public async Task<ConversionResult> ConvertAsync(
        IEnumerable<Asset> stocks,
        IEnumerable<Asset> bonds,
        Asset contribution,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _rateProvider.GetRatesAsync(cancellationToken);

        if (snapshot.Usd == null || snapshot.Eur == null)
        {
            throw new ExchangeRatesUnavailableException();
        }

        var stocksAmount = stocks.Sum(x => ConvertToRub(x, snapshot.Usd, snapshot.Eur));
        var bondsAmount = bonds.Sum(x => ConvertToRub(x, snapshot.Usd, snapshot.Eur));
        var contributionAmount = ConvertToRub(contribution, snapshot.Usd, snapshot.Eur);

        var resultCurrency = SupportedCurrency.Normalize(contribution.Currency);
        var convertedStocksAmount = ConvertFromRub(resultCurrency, stocksAmount, snapshot.Usd, snapshot.Eur);
        var convertedBondsAmount = ConvertFromRub(resultCurrency, bondsAmount, snapshot.Usd, snapshot.Eur);
        var convertedContributionAmount = ConvertFromRub(resultCurrency, contributionAmount, snapshot.Usd, snapshot.Eur);

        return new ConversionResult(
            convertedStocksAmount,
            convertedBondsAmount,
            convertedContributionAmount,
            ToFxMetadata(snapshot));
    }

    public async Task<RatesResponse> GetRatesResponseAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _rateProvider.GetRatesAsync(cancellationToken);
        if (snapshot.Usd == null || snapshot.Eur == null)
        {
            throw new ExchangeRatesUnavailableException();
        }

        return new RatesResponse
        {
            Source = snapshot.Source,
            RatesAsOf = snapshot.RatesAsOf,
            FromCache = snapshot.FromCache,
            Stale = snapshot.Stale,
            RatesPerUnitInRub = BuildRatesPerUnit(snapshot.Usd, snapshot.Eur)
        };
    }

    public async Task<bool> AreRatesAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _rateProvider.GetRatesAsync(cancellationToken);
            return snapshot.Usd != null && snapshot.Eur != null;
        }
        catch (ExchangeRatesUnavailableException)
        {
            return false;
        }
    }

    internal static decimal ConvertToRub(Asset asset, Currency usd, Currency eur)
    {
        return SupportedCurrency.Normalize(asset.Currency) switch
        {
            "usd" => asset.Value * RatePerUnit(usd),
            "eur" => asset.Value * RatePerUnit(eur),
            _ => asset.Value
        };
    }

    internal static decimal ConvertFromRub(string resultCurrency, decimal value, Currency usd, Currency eur)
    {
        return resultCurrency switch
        {
            "usd" => value / RatePerUnit(usd),
            "eur" => value / RatePerUnit(eur),
            _ => value
        };
    }

    internal static decimal RatePerUnit(Currency currency) =>
        currency.Value / currency.Nominal;

    private static FxMetadata ToFxMetadata(RateSnapshot snapshot) =>
        new()
        {
            Source = snapshot.Source,
            RatesAsOf = snapshot.RatesAsOf,
            FromCache = snapshot.FromCache,
            Stale = snapshot.Stale,
            RatesPerUnitInRub = snapshot.Usd != null && snapshot.Eur != null
                ? BuildRatesPerUnit(snapshot.Usd, snapshot.Eur)
                : new Dictionary<string, decimal>()
        };

    private static Dictionary<string, decimal> BuildRatesPerUnit(Currency usd, Currency eur) =>
        new()
        {
            ["rub"] = 1m,
            ["usd"] = RatePerUnit(usd),
            ["eur"] = RatePerUnit(eur)
        };
}
