// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Arc.Commands;

/// <summary>
/// Parses the explicit-offset formats shared by the date-time converters.
/// </summary>
internal static class ExplicitOffsetValue
{
    static readonly string[] _formats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mmzzz",
        "yyyy-MM-dd'T'HH:mm'Z'"
    ];

    /// <summary>
    /// Reads an instant without assuming a local offset.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The parsed instant with its original offset.</returns>
    /// <exception cref="JsonException">The token is not a string.</exception>
    /// <exception cref="ValueRequiresExplicitOffset">The string is not a supported explicit-offset instant.</exception>
    internal static DateTimeOffset Read(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException();
        }

        if (!DateTimeOffset.TryParseExact(reader.GetString(), _formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var instant))
        {
            throw new ValueRequiresExplicitOffset();
        }

        return instant;
    }
}
