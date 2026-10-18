// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.Commands;

/// <summary>
/// Binds an explicitly offset instant through the configured concept metadata.
/// </summary>
/// <param name="conceptType">The concrete concept type.</param>
/// <typeparam name="TValue">The date-time primitive wrapped by the concept.</typeparam>
internal sealed class ExplicitOffsetConceptConverter<TValue>(Type conceptType) : JsonConverter<ConceptAs<TValue>>
    where TValue : struct, IComparable
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert == conceptType;

    /// <inheritdoc/>
    public override ConceptAs<TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var instant = ExplicitOffsetValue.Read(ref reader);
        var text = typeof(TValue) == typeof(DateTimeOffset)
            ? instant.ToString("O", CultureInfo.InvariantCulture)
            : instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        var normalized = new Utf8JsonReader(Encoding.UTF8.GetBytes($"\"{text}\""));

        return (ConceptAs<TValue>?)JsonSerializer.Deserialize(ref normalized, options.ResolveTypeInfo(conceptType));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ConceptAs<TValue> value, JsonSerializerOptions options)
    {
        if (value.Value is DateTime dateTime)
        {
            writer.WriteStringValue(dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime());
        }
        else
        {
            writer.WriteStringValue((DateTimeOffset)(object)value.Value);
        }
    }
}
