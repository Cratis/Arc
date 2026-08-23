// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Queries;

/// <summary>
/// Tracks subscription states for one multiplexed connection and bounds retained revision tombstones.
/// </summary>
/// <param name="getUtcNow">The clock used for retention decisions.</param>
/// <remarks>
/// Revision tombstones are retained for <see cref="TombstoneRetention"/> so delayed subscribe and unsubscribe
/// operations remain ordered. The oldest tombstones are discarded if <see cref="MaximumRetainedTombstones"/> is
/// exceeded, bounding memory for a connection under adversarial unique query identifiers. Active subscriptions are
/// never candidates for cleanup. All removal compares the dictionary value by instance so a replaced state cannot be
/// removed accidentally.
/// </remarks>
internal sealed class ObservableQuerySubscriptionStates(Func<DateTimeOffset>? getUtcNow = null) : IDisposable
{
    /// <summary>
    /// The maximum number of revision tombstones retained for one connection.
    /// </summary>
    internal const int MaximumRetainedTombstones = 1024;

    /// <summary>
    /// The out-of-order operation window preserved for revision tombstones.
    /// </summary>
    internal static readonly TimeSpan TombstoneRetention = TimeSpan.FromMinutes(2);

    readonly ConcurrentDictionary<string, ObservableQuerySubscriptionState> _states = new();
    readonly object _sync = new();
    readonly Func<DateTimeOffset> _getUtcNow = getUtcNow ?? (() => DateTimeOffset.UtcNow);

    /// <summary>
    /// Gets the total number of retained states.
    /// </summary>
    public int Count => _states.Count;

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    public int ActiveCount => _states.Values.Count(_ => _.IsActive);

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_sync)
        {
            foreach (var state in _states.Values)
            {
                state.Dispose();
            }

            _states.Clear();
        }
    }

    /// <summary>
    /// Attempts to reserve a subscribe operation.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="revision">The optional protocol revision.</param>
    /// <param name="connectionToken">The connection cancellation token.</param>
    /// <returns>The operation when accepted; otherwise, <see langword="null"/>.</returns>
    internal ObservableQuerySubscriptionOperation? TrySubscribe(
        string queryId,
        long? revision,
        CancellationToken connectionToken)
    {
        lock (_sync)
        {
            Cleanup(_getUtcNow());
            var state = _states.GetOrAdd(queryId, static _ => new ObservableQuerySubscriptionState());
            var operation = state.TrySubscribe(revision, connectionToken);
            Cleanup(_getUtcNow());

            return operation;
        }
    }

    /// <summary>
    /// Applies an unsubscribe operation.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="revision">The optional protocol revision.</param>
    /// <returns><see langword="true"/> when accepted.</returns>
    internal bool TryUnsubscribe(string queryId, long? revision)
    {
        lock (_sync)
        {
            var now = _getUtcNow();
            Cleanup(now);

            ObservableQuerySubscriptionState state;
            if (revision is not null)
            {
                state = _states.GetOrAdd(queryId, static _ => new ObservableQuerySubscriptionState());
            }
            else if (!_states.TryGetValue(queryId, out state!))
            {
                return false;
            }

            var accepted = state.TryUnsubscribe(revision, now);
            Cleanup(now);

            return accepted;
        }
    }

    /// <summary>
    /// Determines whether an operation still owns a query.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="operation">The operation.</param>
    /// <returns><see langword="true"/> when current.</returns>
    internal bool IsCurrent(string queryId, ObservableQuerySubscriptionOperation operation) =>
        _states.TryGetValue(queryId, out var state) && state.IsCurrent(operation);

    /// <summary>
    /// Terminates an operation if it still owns the query.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="operation">The operation.</param>
    internal void Terminate(string queryId, ObservableQuerySubscriptionOperation operation)
    {
        lock (_sync)
        {
            if (_states.TryGetValue(queryId, out var state))
            {
                state.TryTerminate(operation, _getUtcNow());
            }

            Cleanup(_getUtcNow());
        }
    }

    /// <summary>
    /// Removes expired and excess tombstones.
    /// </summary>
    internal void Cleanup()
    {
        lock (_sync)
        {
            Cleanup(_getUtcNow());
        }
    }

    void Cleanup(DateTimeOffset now)
    {
        var tombstones = new List<(string QueryId, ObservableQuerySubscriptionState State, DateTimeOffset TombstonedAt)>();

        foreach (var (queryId, state) in _states)
        {
            if (state.IsInactiveLegacy)
            {
                Remove(queryId, state);
                continue;
            }

            if (state.TryGetTombstonedAt(out var tombstonedAt))
            {
                if (now - tombstonedAt >= TombstoneRetention)
                {
                    Remove(queryId, state);
                }
                else
                {
                    tombstones.Add((queryId, state, tombstonedAt));
                }
            }
        }

        if (tombstones.Count <= MaximumRetainedTombstones)
        {
            return;
        }

        foreach (var tombstone in tombstones
                     .OrderBy(_ => _.TombstonedAt)
                     .ThenBy(_ => _.QueryId, StringComparer.Ordinal)
                     .Take(tombstones.Count - MaximumRetainedTombstones))
        {
            Remove(tombstone.QueryId, tombstone.State);
        }
    }

    void Remove(string queryId, ObservableQuerySubscriptionState state) =>
        _states.TryRemove(new KeyValuePair<string, ObservableQuerySubscriptionState>(queryId, state));
}
