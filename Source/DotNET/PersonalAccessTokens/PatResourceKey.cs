// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>A canonical PAT resource-key shape; this does not establish a resource's registration or binding.</summary>
internal sealed record PatResourceKey : ConceptAs<string>
{
    /// <summary>Initializes a canonical resource key without normalization.</summary>
    /// <param name="value">The resource key.</param>
    internal PatResourceKey(string value) : base(Validate(value))
    {
    }

    /// <summary>Checks canonical resource-key syntax only.</summary>
    /// <param name="value">The candidate key.</param>
    /// <returns>Whether the key has a supported canonical shape.</returns>
    internal static bool IsValid(string? value)
    {
        if (string.Equals(value, "direct", StringComparison.Ordinal) || string.Equals(value, "studio", StringComparison.Ordinal)) return true;
        if (value?.StartsWith("stage-", StringComparison.Ordinal) != true) return false;
        return IsValidAppKey(value.AsSpan(6));
    }

    static bool IsValidAppKey(ReadOnlySpan<char> value)
    {
        if (value.Length is < 1 or > 48) return false;
        if (!IsLetterOrDigit(value[0]) || !IsLetterOrDigit(value[^1])) return false;
        foreach (var character in value)
        {
            if (!IsLetterOrDigit(character) && character != '-') return false;
        }
        return true;
    }

    static bool IsLetterOrDigit(char character) => character is (>= 'a' and <= 'z') or (>= '0' and <= '9');

    static string Validate(string value) => IsValid(value) ? value : throw new ArgumentException("Invalid PAT resource key.", nameof(value));
}
