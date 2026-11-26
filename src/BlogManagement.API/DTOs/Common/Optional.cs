using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlogManagement.API.DTOs.Common;

/// <summary>
/// Distinguishes omitted JSON properties (HasValue = false) from explicitly provided values,
/// including explicit null (HasValue = true, Value = null), for custom PATCH DTOs.
/// </summary>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    public bool HasValue { get; }
    public T? Value { get; }

    public Optional(T? value)
    {
        HasValue = true;
        Value = value;
    }

    public static Optional<T> Undefined => default;

    public static implicit operator Optional<T>(T? value) => new(value);
}

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class OptionalJsonConverter<TInner> : JsonConverter<Optional<TInner>>
    {
        public override Optional<TInner> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new Optional<TInner>(default);
            }

            var value = JsonSerializer.Deserialize<TInner>(ref reader, options);
            return new Optional<TInner>(value);
        }

        public override void Write(Utf8JsonWriter writer, Optional<TInner> value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
