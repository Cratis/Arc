// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Observability;

/// <summary>
/// Caps how many distinct values a metric attribute can take, folding every value past the limit into one.
/// </summary>
/// <param name="limit">The number of distinct values to let through.</param>
/// <remarks>
/// <para>
/// Command types and query names are bounded by the application, but a metric backend pays for every distinct
/// combination it ever sees, so the bound is enforced here rather than trusted.
/// </para>
/// <para>
/// The limiter takes no lock. A value already let through is found with a lock-free read, and a new value reserves
/// its slot with an interlocked increment before it is added, so no more than <paramref name="limit"/> distinct
/// values are ever let through, however many threads record at once. Close to the limit, a new value can be folded
/// once while another thread holds a slot it gives back; it is let through the next time if a slot is still free.
/// </para>
/// </remarks>
internal sealed class CardinalityLimiter(int limit)
{
    readonly ConcurrentDictionary<string, byte> _seen = new(StringComparer.Ordinal);
    int _count;

    /// <summary>
    /// Gets the number of distinct values let through.
    /// </summary>
    internal int Count => Volatile.Read(ref _count);

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

        if (Volatile.Read(ref _count) >= limit)
        {
            return WellKnownTelemetryNames.Other;
        }

        if (Interlocked.Increment(ref _count) > limit)
        {
            Interlocked.Decrement(ref _count);
            return _seen.ContainsKey(value) ? value : WellKnownTelemetryNames.Other;
        }

        if (!_seen.TryAdd(value, 0))
        {
            // Another thread added the same value first and holds its own slot for it.
            Interlocked.Decrement(ref _count);
        }

        return value;
    }
}
