// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using Cratis.Concepts;

[assembly: InternalsVisibleTo("Cratis.Arc.Chronicle.Testing")]

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Compares generated identity values without relying on record display strings.
/// </summary>
internal static class GeneratedIdentifierCanonicalizer
{
    /// <summary>
    /// Unwraps concepts and formats supported identity primitives.
    /// </summary>
    /// <param name="value">The generated value.</param>
    /// <returns>The canonical identity, or null for a value that is not an identity primitive.</returns>
    internal static string? Canonicalize(object value)
    {
        while (value.IsConcept())
        {
            value = value.GetConceptValue();
        }

        return value switch
        {
            Guid identifier => identifier.ToString("D"),
            string identifier => identifier,
            DateTime identifier => identifier.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset identifier => identifier.ToString("O", CultureInfo.InvariantCulture),
            DateOnly identifier => identifier.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly identifier => identifier.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan identifier => identifier.ToString("c", CultureInfo.InvariantCulture),
            IFormattable identifier when value.GetType().IsPrimitive || value is decimal || value.GetType().IsEnum => identifier.ToString(null, CultureInfo.InvariantCulture),
            bool identifier => identifier.ToString(),
            char identifier => identifier.ToString(),
            _ => null
        };
    }
}
