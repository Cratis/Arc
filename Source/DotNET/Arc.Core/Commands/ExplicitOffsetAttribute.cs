// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Commands;

/// <summary>
/// Requires a JSON date-time string with an explicit offset or Z, preserving tick precision.
/// </summary>
/// <remarks>
/// DateTimeOffset retains its offset; DateTime is converted to UTC. On a positional record use the property target.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ExplicitOffsetAttribute : JsonConverterAttribute
{
    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert) => typeToConvert switch
    {
        _ when typeToConvert == typeof(DateTimeOffset) => new ExplicitOffsetDateTimeOffsetConverter(),
        _ when typeToConvert == typeof(DateTimeOffset?) => new ExplicitOffsetNullableDateTimeOffsetConverter(),
        _ when typeToConvert == typeof(DateTime) => new ExplicitOffsetDateTimeConverter(),
        _ when typeToConvert == typeof(DateTime?) => new ExplicitOffsetNullableDateTimeConverter(),
        _ => ConceptConverter(typeToConvert)
    };

    static JsonConverter ConceptConverter(Type typeToConvert) => typeToConvert switch
    {
        _ when typeof(ConceptAs<DateTimeOffset>).IsAssignableFrom(typeToConvert) => new ExplicitOffsetConceptConverter<DateTimeOffset>(typeToConvert),
        _ when typeof(ConceptAs<DateTime>).IsAssignableFrom(typeToConvert) => new ExplicitOffsetConceptConverter<DateTime>(typeToConvert),
        _ => throw new ExplicitOffsetRequiresDateTimeOrDateTimeOffset(typeToConvert)
    };
}
