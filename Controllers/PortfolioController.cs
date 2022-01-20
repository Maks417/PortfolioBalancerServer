using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Calculate([FromBody] CalculationData formData)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray();
                return BadRequest(errors);
            }

            var (stocksAmount, bondsAmount, contributionAmount) = await _currencyConverter.Convert(formData.StockValues, formData.BondValues, formData.ContributionAmount);

            var (firstRatio, secondRatio) = _calculationService.ParseRatio(formData.Ratio);
            if (firstRatio == decimal.Zero)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var assetsDiff = _calculationService.SplitAssetsByRatio(stocksAmount, bondsAmount, contributionAmount, firstRatio, secondRatio);
            assetsDiff.Currency = formData.ContributionAmount.Currency;

            return Ok(assetsDiff);
        }
    }
}
