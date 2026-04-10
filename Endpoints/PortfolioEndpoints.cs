using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Services;

namespace PortfolioBalancerServer.Endpoints
{
    public static class PortfolioEndpoints
    {
        public static void MapPortfolioEndpoints(this WebApplication app)
        {
            app.MapPost("api/portfolio/calculate", HandleCalculateAsync);
        }

        private static async Task<IResult> HandleCalculateAsync(
            ICurrencyConverter currencyConverter,
            ICalculationService calculationService,
            [FromBody] CalculationData formData)
        {
            if (!MiniValidator.TryValidate(formData, out var errors))
            {
                return Results.BadRequest(errors.Values);
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
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var (firstRatio, secondRatio) = calculationService.ParseRatio(formData.Ratio);
            if (firstRatio == decimal.Zero)
            {
                return Results.BadRequest(new[] { new[] { "Ratio must have 100 in sum for format like '70/30'." } });
            }

            var assetsDiff = calculationService.SplitAssetsByRatio(stocksAmount, bondsAmount, contributionAmount, firstRatio, secondRatio);
            assetsDiff.Currency = formData.ContributionAmount.Currency;

            return Results.Ok(assetsDiff);
        }
    }
}
