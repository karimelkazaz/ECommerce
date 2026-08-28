using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Infrastructure.Persistence.Outbox;

public static class OutboxJsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new MoneyConverter());
        options.Converters.Add(new CurrencyConverter());

        return options;
    }
}

public sealed class MoneyConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);

        var amount = document.RootElement.GetProperty("amount").GetDecimal();
        var currencyCode = document.RootElement.GetProperty("currency").GetString()
            ?? throw new JsonException("Money payload is missing the currency code.");

        return Money.Create(amount, Currency.FromCode(currencyCode)).Value;
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency.Code);
        writer.WriteEndObject();
    }
}

public sealed class CurrencyConverter : JsonConverter<Currency>
{
    public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var code = reader.GetString()
            ?? throw new JsonException("Currency payload is missing the code.");

        return Currency.FromCode(code);
    }

    public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Code);
    }
}
