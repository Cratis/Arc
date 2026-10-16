// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.Commands;

/// <summary>
/// Binds a nullable explicitly offset instant as a UTC DateTime.
/// </summary>
internal sealed class ExplicitOffsetNullableDateTimeConverter : JsonConverter<DateTime?>
{
    /// <inheritdoc/>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : ExplicitOffsetValue.Read(ref reader).UtcDateTime;

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
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
