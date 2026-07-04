using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
        app.MapPost("api/portfolio/calculate", HandleCalculateAsync)
            .Produces<AssetsDiff>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleCalculateAsync(
        ICurrencyConverter currencyConverter,
        ICalculationService calculationService,
        [FromBody] CalculationData formData)
    {
        formData.StockValues = AssetFilter.FilterFilled(formData.StockValues);
        formData.BondValues = AssetFilter.FilterFilled(formData.BondValues);
        formData.ContributionAmount.Currency = SupportedCurrency.Normalize(formData.ContributionAmount.Currency);

        foreach (var asset in formData.StockValues.Concat(formData.BondValues))
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

        decimal stocksAmount;
        decimal bondsAmount;
        decimal contributionAmount;
        try
        {
            (stocksAmount, bondsAmount, contributionAmount) = await currencyConverter.Convert(
                formData.StockValues,
                formData.BondValues,
                formData.ContributionAmount);
        }
        catch (ExchangeRatesUnavailableException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Exchange rates unavailable");
        }

        RatioParser.TryParse(formData.Ratio, out var firstRatio, out var secondRatio);

        var assetsDiff = calculationService.SplitAssetsByRatio(
            stocksAmount,
            bondsAmount,
            contributionAmount,
            firstRatio,
            secondRatio);
        assetsDiff.Currency = formData.ContributionAmount.Currency;

        return Results.Ok(assetsDiff);
    }
}
