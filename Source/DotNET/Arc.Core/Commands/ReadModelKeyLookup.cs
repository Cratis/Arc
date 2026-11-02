// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// A caller-derived lookup result. Its default value is missing, never an empty key.
/// </summary>
/// <param name="Outcome">The lookup outcome.</param>
/// <param name="Key">The key, only meaningful for a found outcome.</param>
public readonly record struct ReadModelKeyLookup(ReadModelKeyLookupOutcome Outcome, object? Key = null)
{
    /// <summary>Gets a missing lookup.</summary>
    public static ReadModelKeyLookup Missing => default;

    /// <summary>Gets an ambiguous lookup.</summary>
    public static ReadModelKeyLookup Ambiguous => new(ReadModelKeyLookupOutcome.Ambiguous);

    /// <summary>Creates a found lookup.</summary>
    /// <param name="key">The resolved key.</param>
    /// <returns>The lookup result.</returns>
    public static ReadModelKeyLookup Found(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new(ReadModelKeyLookupOutcome.Found, key);
    }
}
