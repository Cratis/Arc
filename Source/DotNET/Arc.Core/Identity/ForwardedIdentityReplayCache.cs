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
    readonly HashSet<(string Audience, string KeyId, string Nonce)> _entries = [];
    readonly Dictionary<long, List<(string Audience, string KeyId, string Nonce)>> _expirations = [];
    readonly PriorityQueue<long, long> _expiryOrder = new();

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
            // Timestamps can arrive out of order. Only visit expired second-buckets,
            // never scan live nonces. The verifier permits at most 91 live seconds.
            while (_expiryOrder.TryPeek(out var expiry, out _) && expiry < now)
            {
                _expiryOrder.Dequeue();
                foreach (var key in _expirations[expiry])
                {
                    _entries.Remove(key);
                }
                _expirations.Remove(expiry);
            }
            var replayKey = (audience, envelope.KeyId, envelope.Nonce);
            if (_entries.Count >= capacity || _entries.Contains(replayKey))
            {
                return false;
            }
            var expiresAt = envelope.Timestamp + 60;
            if (!_expirations.TryGetValue(expiresAt, out var bucket))
            {
                bucket = [];
                _expirations.Add(expiresAt, bucket);
                _expiryOrder.Enqueue(expiresAt, expiresAt);
            }
            bucket.Add(replayKey);
            _entries.Add(replayKey);
            return true;
        }
    }
}
