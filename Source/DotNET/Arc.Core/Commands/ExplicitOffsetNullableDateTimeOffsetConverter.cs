// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.Commands;

/// <summary>
/// Binds a nullable explicitly offset instant while preserving its offset.
/// </summary>
internal sealed class ExplicitOffsetNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    /// <inheritdoc/>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : ExplicitOffsetValue.Read(ref reader);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is { } instant)
        {
            writer.WriteStringValue(instant);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
