// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Atomically reserves nonces process-locally without evicting live entries.
/// </summary>
/// <param name="capacity">The maximum live entry count.</param>
internal sealed class ForwardedIdentityReplayCache(int capacity = 100000)
{
    /// <summary>
    /// The process-wide production cache.
    /// </summary>
    internal static readonly ForwardedIdentityReplayCache Shared = new();
    readonly object _gate = new();
    readonly Dictionary<(string Audience, string KeyId, string Nonce), long> _entries = [];

    /// <summary>
    /// Reserves a verified identity envelope through its last acceptable second, inclusive.
    /// </summary>
    /// <param name="audience">The verified audience.</param>
    /// <param name="envelope">The verified envelope.</param>
    /// <param name="now">The current UTC Unix seconds.</param>
    /// <returns>Whether a new reservation was made.</returns>
    internal bool TryReserve(string audience, ForwardedIdentityEnvelope envelope, long now)
    {
        lock (_gate)
        {
            foreach (var key in _entries.Where(entry => entry.Value < now).Select(entry => entry.Key).ToArray())
            {
                _entries.Remove(key);
            }
            var replayKey = (audience, envelope.KeyId, envelope.Nonce);
            if (_entries.Count >= capacity || _entries.ContainsKey(replayKey))
            {
                return false;
            }
            _entries.Add(replayKey, envelope.Timestamp + 60);
            return true;
        }
    }
}
