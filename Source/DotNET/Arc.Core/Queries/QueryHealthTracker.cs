// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IQueryHealthTracker"/>.
/// </summary>
[Singleton]
public sealed class QueryHealthTracker : IQueryHealthTracker, IDisposable
{
    readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    readonly BehaviorSubject<IEnumerable<QueryConnectionHealth>> _healthSubject = new([]);

    /// <inheritdoc/>
    public void RegisterSubscription(string connectionId, string protocol, QuerySubscriptionMetadata metadata)
    {
        var connection = _connections.GetOrAdd(connectionId, (id, proto) => new ConnectionInfo(id, proto, DateTimeOffset.UtcNow), protocol);
        connection.Subscriptions[metadata.SubscriptionId] = metadata;
        PublishUpdate();
    }

    /// <inheritdoc/>
    public void UnregisterSubscription(string connectionId, string subscriptionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.Subscriptions.TryRemove(subscriptionId, out _);

            // If no more subscriptions, remove the connection after a short delay
            if (connection.Subscriptions.IsEmpty)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    if (connection.Subscriptions.IsEmpty)
                    {
                        _connections.TryRemove(connectionId, out _);
                        PublishUpdate();
                    }
                });
            }

            PublishUpdate();
        }
    }

    /// <inheritdoc/>
    public void RecordPingSent(string connectionId, string subscriptionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection) &&
            connection.Subscriptions.TryGetValue(subscriptionId, out var metadata))
        {
            metadata.LastPingSentAt = DateTimeOffset.UtcNow;
            PublishUpdate();
        }
    }

    /// <inheritdoc/>
    public void RecordPongReceived(string connectionId, string subscriptionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection) &&
            connection.Subscriptions.TryGetValue(subscriptionId, out var metadata))
        {
            metadata.LastPongReceivedAt = DateTimeOffset.UtcNow;
            PublishUpdate();
        }
    }

    /// <inheritdoc/>
    public void RecordDataServed(string connectionId, string subscriptionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection) &&
            connection.Subscriptions.TryGetValue(subscriptionId, out var metadata))
        {
            metadata.LastDataServedAt = DateTimeOffset.UtcNow;
            PublishUpdate();
        }
    }

    /// <inheritdoc/>
    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        PublishUpdate();
    }

    /// <inheritdoc/>
    public IEnumerable<QueryConnectionHealth> GetAllConnectionHealth()
    {
        return _connections.Values.Select(connection => new QueryConnectionHealth
        {
            ConnectionId = connection.ConnectionId,
            Protocol = connection.Protocol,
            EstablishedAt = connection.EstablishedAt,
            Subscriptions = connection.Subscriptions.Values.ToList()
        });
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<QueryConnectionHealth>> ObserveHealth()
    {
        var newSubject = new BehaviorSubject<IEnumerable<QueryConnectionHealth>>(_healthSubject.Value);
        _healthSubject.Subscribe(newSubject);
        return newSubject;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _healthSubject?.Dispose();
    }

    void PublishUpdate()
    {
        _healthSubject.OnNext(GetAllConnectionHealth());
    }

    sealed record ConnectionInfo(string ConnectionId, string Protocol, DateTimeOffset EstablishedAt)
    {
        public ConcurrentDictionary<string, QuerySubscriptionMetadata> Subscriptions { get; } = new();
    }
}
