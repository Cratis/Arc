// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>
/// Provides a bounded single-process replay guarantee without evicting live entries.
/// Fleet-wide durable reservation follows in Cratis/Arc#3707; this store is never an automatic production fallback.
/// </summary>
/// <param name="capacity">The maximum live reservation count.</param>
internal sealed class ProcessLocalForwardedIdentityNonceStore(int capacity = 100000) : IForwardedIdentityNonceStore
{
    readonly object _gate = new();
    readonly HashSet<(string Audience, string KeyId, string Nonce)> _entries = [];
    readonly Dictionary<decimal, List<(string Audience, string KeyId, string Nonce)>> _expirations = [];
    readonly PriorityQueue<decimal, decimal> _expiryOrder = new();

    /// <inheritdoc/>
    Task<ForwardedIdentityNonceReservation> IForwardedIdentityNonceStore.Reserve(string aud, string kid, string nonce, long timestamp, long now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            while (_expiryOrder.TryPeek(out var expiry, out _) && expiry < now)
            {
                _expiryOrder.Dequeue();
                foreach (var key in _expirations[expiry]) _entries.Remove(key);
                _expirations.Remove(expiry);
            }
            var replayKey = (aud, kid, nonce);
            if (_entries.Contains(replayKey)) return Task.FromResult(ForwardedIdentityNonceReservation.Replayed);
            if (_entries.Count >= capacity) return Task.FromResult(ForwardedIdentityNonceReservation.Unavailable);
            var expiresAt = (decimal)timestamp + 60;
            if (!_expirations.TryGetValue(expiresAt, out var bucket))
            {
                bucket = [];
                _expirations.Add(expiresAt, bucket);
                _expiryOrder.Enqueue(expiresAt, expiresAt);
            }
            bucket.Add(replayKey);
            _entries.Add(replayKey);
            return Task.FromResult(ForwardedIdentityNonceReservation.Reserved);
        }
    }
}
