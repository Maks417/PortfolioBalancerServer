using System.ComponentModel.DataAnnotations;
using PortfolioBalancerServer.Domain;

namespace PortfolioBalancerServer.Models
{
    public record CalculationData : IValidatableObject
    {
        [Required, MinLength(3)]
        public required string Ratio { get; set; }

        [Required]
        public required Asset[] StockValues { get; set; }

        [Required]
        public required Asset[] BondValues { get; set; }

        [Required]
        public required Asset ContributionAmount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!RatioParser.TryParse(Ratio, out _, out _))
            {
                yield return new ValidationResult("Ratio must have format like '70/30' or '100' with parts summing to 100.");
            }
        }
    }

    public record Asset : IValidatableObject
    {
        [Required, Range(0, double.PositiveInfinity)]
        public decimal Value { get; set; }

        [Required, MinLength(1)]
        public required string Currency { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value < 0)
            {
                yield return new ValidationResult("Asset value must be positive.");
            }

            if (string.IsNullOrEmpty(Currency) || Currency.Length <= 1)
            {
                yield return new ValidationResult("Currency must follow ISO 4217 code");
            }
        }
    }
}
