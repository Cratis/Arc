// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an <see cref="ICachingConnectionResolver"/> that keeps resolved connections in memory.
/// </summary>
/// <remarks>
/// Entries are keyed by the request — storage, purpose and scope — never by credentials, and are kept until the
/// <see cref="ResolvedConnection.ExpiresAt"/> the resolver gave, less a minute of margin, or for the configured duration
/// when it gave none. Concurrent requests for the same key share one resolution. A failed resolution is not kept.
/// </remarks>
/// <param name="inner">The <see cref="IConnectionResolver"/> to resolve through.</param>
/// <param name="cacheDuration">How long to keep a resolution without an expiry.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> for the current time.</param>
public class CachingConnectionResolver(IConnectionResolver inner, TimeSpan cacheDuration, TimeProvider timeProvider) : ICachingConnectionResolver
{
    /// <summary>
    /// The margin before <see cref="ResolvedConnection.ExpiresAt"/> at which a resolution is renewed.
    /// </summary>
    public static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(1);

    readonly ConcurrentDictionary<string, Lazy<Task<Entry>>> _entries = new();

    /// <inheritdoc/>
    public async Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (cacheDuration <= TimeSpan.Zero)
        {
            return await inner.Resolve(request, cancellationToken);
        }

        var key = request.ToKey();
        var (source, entry) = await GetOrResolve(key, request, cancellationToken);
        if (entry.ValidUntil > timeProvider.GetUtcNow())
        {
            return entry.Connection;
        }

        // Renew once. What comes back is used even when it is already within the expiry margin, rather than asking the
        // resolver again in a loop for a credential it cannot make last longer.
        _entries.TryRemove(new(key, source));
        (_, entry) = await GetOrResolve(key, request, cancellationToken);
        return entry.Connection;
    }

    /// <inheritdoc/>
    public void Invalidate(ConnectionRequest request) => _entries.TryRemove(request.ToKey(), out _);

    async Task<(Lazy<Task<Entry>> Source, Entry Entry)> GetOrResolve(string key, ConnectionRequest request, CancellationToken cancellationToken)
    {
        var lazy = _entries.GetOrAdd(key, static (_, state) => new Lazy<Task<Entry>>(() => state.Self.ResolveEntry(state.Request)), (Self: this, Request: request));
        try
        {
            return (lazy, await lazy.Value.WaitAsync(cancellationToken));
        }
        catch when (lazy.Value.IsFaulted || lazy.Value.IsCanceled)
        {
            _entries.TryRemove(new(key, lazy));
            throw;
        }
    }

    async Task<Entry> ResolveEntry(ConnectionRequest request)
    {
        var connection = await inner.Resolve(request, CancellationToken.None);
        var validUntil = connection.ExpiresAt is { } expiresAt
            ? expiresAt - ExpiryMargin
            : timeProvider.GetUtcNow() + cacheDuration;
        return new(connection, validUntil);
    }

    sealed record Entry(ResolvedConnection Connection, DateTimeOffset ValidUntil);
}
