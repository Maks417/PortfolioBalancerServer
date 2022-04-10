using System.ComponentModel.DataAnnotations;

namespace PortfolioBalancerServer.Models
{
    public record CalculationData : IValidatableObject
    {
        [Required, MinLength(3)]
        public string Ratio { get; set; }

        [Required]
        public Asset[] StockValues { get; set; }

        [Required]
        public Asset[] BondValues { get; set; }

        [Required]
        public Asset ContributionAmount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Ratio) 
                || (Ratio.Length == 3 && !Ratio.Equals("100", StringComparison.InvariantCultureIgnoreCase)))
            {
                yield return new ("Ratio must have format like '70/30' or '100'.");
            }
        }
    }

    public record Asset : IValidatableObject
    {
        [Required, Range(0, double.PositiveInfinity)]
        public decimal Value { get; set; }

        [Required, MinLength(1)]
        public string Currency { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value < 0)
            {
                yield return new ("Asset value must be positive.");
            }

            if (string.IsNullOrEmpty(Currency) || Currency.Length <= 1)
            {
                yield return new("Currency must follow ISO 4217 code");
            }
        }
    }
}
