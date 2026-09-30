// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Observability;

/// <summary>
/// Caps how many distinct values a metric attribute can take, folding every value past the limit into one.
/// </summary>
/// <param name="limit">The number of distinct values to let through.</param>
/// <remarks>
/// Command types and query names are bounded by the application, but a metric backend pays for every distinct
/// combination it ever sees, so the bound is enforced here rather than trusted.
/// </remarks>
internal sealed class CardinalityLimiter(int limit)
{
    readonly ConcurrentDictionary<string, byte> _seen = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the value to record for the given value.
    /// </summary>
    /// <param name="value">The value to record.</param>
    /// <returns>The value itself while under the limit or already seen; otherwise <see cref="WellKnownTelemetryNames.Other"/>.</returns>
    internal string Limit(string value)
    {
        if (_seen.ContainsKey(value))
        {
            return value;
        }

        if (_seen.Count >= limit)
        {
            return WellKnownTelemetryNames.Other;
        }

        _seen.TryAdd(value, 0);
        return value;
    }
}
