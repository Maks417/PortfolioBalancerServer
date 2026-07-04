using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortfolioBalancerServer.Serialization;

public sealed class FlexibleDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ParseString(reader.GetString()),
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.Null => decimal.Zero,
            _ => throw new JsonException($"Cannot convert token {reader.TokenType} to decimal.")
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

    private static decimal ParseString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return decimal.Zero;
        }

        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}
