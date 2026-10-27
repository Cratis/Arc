// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Identity;

/// <summary>
/// Remembers the <c>jti</c> of every accepted on-behalf assertion until it expires, so an assertion is accepted once.
/// </summary>
/// <remarks>
/// The cache is in memory and per process. With several instances of the application, an assertion replayed to a
/// different instance within its lifetime is not detected; the short lifetime of the assertion bounds that window.
/// </remarks>
internal sealed class OnBehalfOfReplayCache
{
    const int PurgeEvery = 1024;

    readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new(StringComparer.Ordinal);
    int _recorded;

    /// <summary>
    /// Gets the cache shared by every authentication handler in the process.
    /// </summary>
    internal static OnBehalfOfReplayCache Shared { get; } = new();

    /// <summary>
    /// Records a token identifier, unless it has already been recorded and not yet expired.
    /// </summary>
    /// <param name="tokenId">The token identifier.</param>
    /// <param name="keepUntil">When the identifier may be forgotten.</param>
    /// <param name="now">The current time.</param>
    /// <returns>True when the identifier was recorded; false when it is a replay.</returns>
    internal bool TryRecord(string tokenId, DateTimeOffset keepUntil, DateTimeOffset now)
    {
        if (Interlocked.Increment(ref _recorded) % PurgeEvery == 0)
        {
            Purge(now);
        }

        while (true)
        {
            if (_seen.TryAdd(tokenId, keepUntil))
            {
                return true;
            }

            if (!_seen.TryGetValue(tokenId, out var until))
            {
                continue;
            }

            if (until > now)
            {
                return false;
            }

            if (_seen.TryUpdate(tokenId, keepUntil, until))
            {
                return true;
            }
        }
    }

    void Purge(DateTimeOffset now)
    {
        foreach (var (tokenId, until) in _seen)
        {
            if (until <= now)
            {
                _seen.TryRemove(new KeyValuePair<string, DateTimeOffset>(tokenId, until));
            }
        }
    }
}
