using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniValidation;
using PortfolioBalancerServer.Domain;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Services;

namespace PortfolioBalancerServer.Endpoints;

public static class PortfolioEndpoints
{
    public static void MapPortfolioEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/portfolio")
            .RequireRateLimiting("portfolio");

        group.MapGet("rates", HandleRatesAsync)
            .Produces<RatesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("calculate", HandleCalculateAsync)
            .Produces<AssetsDiff>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleRatesAsync(
        ICurrencyConverter currencyConverter,
        CancellationToken cancellationToken)
    {
        try
        {
            var rates = await currencyConverter.GetRatesResponseAsync(cancellationToken);
            return Results.Ok(rates);
        }
        catch (ExchangeRatesUnavailableException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Exchange rates unavailable");
        }
    }

    private static async Task<IResult> HandleCalculateAsync(
        ICurrencyConverter currencyConverter,
        ICalculationService calculationService,
        [FromBody] CalculationData formData,
        CancellationToken cancellationToken)
    {
        formData.StockValues = AssetFilter.FilterFilled(formData.StockValues);
        formData.BondValues = AssetFilter.FilterFilled(formData.BondValues);
        formData.CashValues = AssetFilter.FilterFilled(formData.CashValues);
        formData.ContributionAmount.Currency = SupportedCurrency.Normalize(formData.ContributionAmount.Currency);
        formData.Mode = string.IsNullOrWhiteSpace(formData.Mode)
            ? CalculationModes.Contribution
            : formData.Mode.Trim().ToLowerInvariant();

        foreach (var asset in formData.StockValues.Concat(formData.BondValues).Concat(formData.CashValues))
        {
            asset.Currency = SupportedCurrency.Normalize(asset.Currency);
        }

        if (!MiniValidator.TryValidate(formData, out var errors))
        {
            var camelCaseErrors = errors.ToDictionary(
                entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                entry => entry.Value);
            return Results.ValidationProblem(camelCaseErrors);
        }

        ConversionResult conversion;
        try
        {
            conversion = await currencyConverter.ConvertAsync(
                formData.StockValues,
                formData.BondValues,
                formData.CashValues,
                formData.ContributionAmount,
                cancellationToken);
        }
        catch (ExchangeRatesUnavailableException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Exchange rates unavailable");
        }

        RatioParser.TryParseParts(formData.Ratio, out var targetRatios);

        var assetsDiff = calculationService.SplitAssetsByRatio(
            conversion.StocksAmount,
            conversion.BondsAmount,
            conversion.CashAmount,
            conversion.ContributionAmount,
            targetRatios,
            formData.Mode,
            formData.DriftThreshold);
        assetsDiff.Currency = formData.ContributionAmount.Currency;
        assetsDiff.Fx = conversion.Fx;

        var resultCurrency = formData.ContributionAmount.Currency;
        var usd = new Currency { Nominal = 1, Value = conversion.Fx!.RatesPerUnitInRub["usd"] };
        var eur = new Currency { Nominal = 1, Value = conversion.Fx.RatesPerUnitInRub["eur"] };

        decimal ToRubAmount(decimal amount, string currency) =>
            CurrencyConverter.ConvertToRub(
                new Asset { Value = amount, Currency = currency },
                usd,
                eur);

        decimal FromRubAmount(decimal amount, string currency) =>
            CurrencyConverter.ConvertFromRub(currency, amount, usd, eur);

        var isRebalance = string.Equals(formData.Mode, CalculationModes.Rebalance, StringComparison.OrdinalIgnoreCase);
        assetsDiff.StocksBreakdown = ApplyBreakdown(
            formData.StockValues,
            assetsDiff.StocksDiff,
            isRebalance,
            resultCurrency,
            formData.MinTradeAmount,
            ToRubAmount,
            FromRubAmount);
        assetsDiff.BondsBreakdown = ApplyBreakdown(
            formData.BondValues,
            assetsDiff.BondsDiff,
            isRebalance,
            resultCurrency,
            formData.MinTradeAmount,
            ToRubAmount,
            FromRubAmount);
        assetsDiff.CashBreakdown = ApplyBreakdown(
            formData.CashValues,
            assetsDiff.CashDiff,
            isRebalance,
            resultCurrency,
            formData.MinTradeAmount,
            ToRubAmount,
            FromRubAmount);

        return Results.Ok(assetsDiff);
    }

    private static PositionAllocation[] ApplyBreakdown(
        IReadOnlyList<Asset> rows,
        decimal classDiff,
        bool isRebalance,
        string resultCurrency,
        decimal? minTradeAmount,
        Func<decimal, string, decimal> toRub,
        Func<decimal, string, decimal> fromRub)
    {
        var breakdown = isRebalance
            ? PositionBreakdown.DistributeRebalance(rows, classDiff, resultCurrency, toRub, fromRub)
            : PositionBreakdown.DistributeBuys(rows, classDiff, resultCurrency, toRub, fromRub);

        return TradeRounding.ApplyMinTrade(breakdown, minTradeAmount, resultCurrency);
    }
}
