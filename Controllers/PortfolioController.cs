using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using System;
using System.Linq;

namespace PortfolioBalancerServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly ICurrencyConverter _currencyConverter;
        private readonly ICalculationService _calculationService;

        public PortfolioController(ICurrencyConverter currencyConverter, ICalculationService calculationService)
        {
            _currencyConverter = currencyConverter;
            _calculationService = calculationService;
        }

        [HttpPost]
        [Route("calculate")]
        public IActionResult Calculate([FromBody] CalculationData formData)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray();
                return BadRequest(errors);
            }

            var (stocksAmount, bondsAmount, contributionAmount) = _currencyConverter.ConvertToRub(formData.StockValues, formData.BondValues, formData.ContributionAmount);

            var (firstRatio, secondRatio) = ParseRatio(formData.Ratio);
            if (firstRatio == decimal.Zero)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var assetsDiff = _calculationService.SplitAssetsByRatio(stocksAmount, bondsAmount, contributionAmount, firstRatio, secondRatio);

            return Ok(assetsDiff);
        }

        private static (decimal, decimal) ParseRatio(string ratio)
        {
            if (string.IsNullOrEmpty(ratio)
                || ratio.Length > 5
                || (!ratio.Equals("100", StringComparison.OrdinalIgnoreCase) && !ratio.Contains("/", StringComparison.OrdinalIgnoreCase)))
            {
                return (decimal.Zero, decimal.Zero);
            }

            if (ratio.Equals("100", StringComparison.OrdinalIgnoreCase))
            {
                return (1, decimal.Zero);
            }

            var ratios = ratio.Split('/');
            if (ratios.Length == 2
                && decimal.TryParse(ratios[0], out var firstRatio)
                && decimal.TryParse(ratios[1], out var secondRatio)
                && firstRatio + secondRatio == 100)
            {
                return (firstRatio / 100, secondRatio / 100);
            }

            return (decimal.Zero, decimal.Zero);

        }
    }
}
