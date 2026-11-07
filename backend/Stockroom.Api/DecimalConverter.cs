using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
namespace Stockroom.Api;

// Money crosses the JSON boundary as decimal text; the client never calculates valuation.
public sealed class DecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var number)) return number;
        if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)) return value;
        throw new JsonException("Use a nonnegative decimal price.");
    }
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("0.00", CultureInfo.InvariantCulture));
}
