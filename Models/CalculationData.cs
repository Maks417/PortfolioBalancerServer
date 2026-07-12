using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PortfolioBalancerServer.Domain;
using PortfolioBalancerServer.Serialization;

namespace PortfolioBalancerServer.Models;

public record CalculationData : IValidatableObject
{
    [Required]
    public required string Ratio { get; set; }

    [Required]
    public required Asset[] StockValues { get; set; }

    [Required]
    public required Asset[] BondValues { get; set; }

    public Asset[] CashValues { get; set; } = [];

    [Required]
    public required Asset ContributionAmount { get; set; }

    public string Mode { get; set; } = CalculationModes.Contribution;

    [Range(0, 50)]
    public decimal? DriftThreshold { get; set; }

    [Range(0, double.PositiveInfinity)]
    public decimal? MinTradeAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!RatioParser.TryParseParts(Ratio, out _))
        {
            yield return new ValidationResult(
                "Ratio must have format like '70/30', '60/30/10', '100', or '0' with parts summing to 100.",
                [nameof(Ratio)]);
        }

        if (AssetFilter.FilterFilled(StockValues).Length == 0
            && AssetFilter.FilterFilled(BondValues).Length == 0
            && AssetFilter.FilterFilled(CashValues).Length == 0)
        {
            yield return new ValidationResult(
                "At least one stock, bond, or cash position with a value greater than zero is required.",
                [nameof(StockValues), nameof(BondValues), nameof(CashValues)]);
        }

        if (!string.Equals(Mode, CalculationModes.Contribution, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Mode, CalculationModes.Rebalance, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Mode must be 'contribution' or 'rebalance'.",
                [nameof(Mode)]);
        }

        if (string.Equals(Mode, CalculationModes.Contribution, StringComparison.OrdinalIgnoreCase)
            && ContributionAmount.Value <= decimal.Zero)
        {
            yield return new ValidationResult(
                "Contribution amount must be greater than zero.",
                [$"{nameof(ContributionAmount)}.{nameof(Asset.Value)}"]);
        }
    }
}

public record Asset : IValidatableObject
{
    [Required]
    [Range(0, double.PositiveInfinity)]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal Value { get; set; }

    [Required]
    [MinLength(3)]
    public required string Currency { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Currency))
        {
            yield return new ValidationResult(
                "Currency is required.",
                [nameof(Currency)]);
            yield break;
        }

        if (!SupportedCurrency.IsSupported(Currency))
        {
            yield return new ValidationResult(
                "Currency must be one of: rub, usd, eur.",
                [nameof(Currency)]);
        }
    }
}
