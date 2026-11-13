// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Identity.Callers;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Identity.Workload;

/// <summary>Bounded positive cache. Keys are credential digests bound to the exact receiving target.</summary>
/// <param name="options">The immutable capacity and deletion-latency bounds.</param>
/// <param name="clock">The expiry clock.</param>
[IgnoreConvention]
internal sealed class WorkloadVerificationCache(WorkloadCallerOptions options, TimeProvider clock)
{
    readonly Dictionary<Key, Entry> _entries = [];
    readonly object _lock = new();

    /// <summary>Finds an unexpired, exact-target positive entry.</summary>
    /// <param name="token">The bearer to hash.</param>
    /// <param name="target">The exact receiving target.</param>
    /// <returns>The verified facts, or none.</returns>
    internal VerifiedCredentialFacts? Find(string token, CallerCredentialTarget target)
    {
        lock (_lock)
        {
            Prune();
            return _entries.TryGetValue(Key.For(token, target), out var entry) ? entry.Facts : null;
        }
    }

    /// <summary>Stores only positive authority within both expiry bounds and the hard capacity.</summary>
    /// <param name="token">The bearer to hash, never retained.</param>
    /// <param name="target">The exact receiving target.</param>
    /// <param name="facts">The verified workload facts.</param>
    /// <param name="podUid">The immutable verified Pod UID.</param>
    /// <param name="tokenExpiry">The token expiry bound.</param>
    /// <param name="verifiedAt">The start of this verification attempt.</param>
    internal void Store(string token, CallerCredentialTarget target, VerifiedCredentialFacts facts, string podUid, DateTimeOffset tokenExpiry, DateTimeOffset verifiedAt)
    {
        var expires = verifiedAt + options.DeletedPodRejectionLatency;
        if (tokenExpiry < expires) expires = tokenExpiry;
        lock (_lock)
        {
            Prune();
            if (expires <= clock.GetUtcNow()) return;
            var key = Key.For(token, target);
            if (!_entries.ContainsKey(key) && _entries.Count >= options.CacheCapacity)
            {
                _entries.Remove(_entries.MinBy(pair => pair.Value.Expires).Key);
            }
            _entries[key] = new(facts, podUid, expires);
        }
    }

    void Prune()
    {
        var now = clock.GetUtcNow();
        foreach (var key in _entries.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray()) _entries.Remove(key);
    }

    sealed record Entry(VerifiedCredentialFacts Facts, string PodUid, DateTimeOffset Expires);

    sealed record Key(string Digest, string Audience, string Resource, string Environment)
    {
        internal static Key For(string token, CallerCredentialTarget target) =>
            new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), target.Audience, target.Resource, target.Environment);
    }
}
