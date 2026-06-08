// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a service for tracking query subscription health.
/// </summary>
public interface IQueryHealthTracker
{
    /// <summary>
    /// Registers a new subscription.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="protocol">The connection protocol (WebSocket or SSE).</param>
    /// <param name="metadata">The subscription metadata.</param>
    void RegisterSubscription(string connectionId, string protocol, QuerySubscriptionMetadata metadata);

    /// <summary>
    /// Unregisters a subscription.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="subscriptionId">The subscription identifier.</param>
    void UnregisterSubscription(string connectionId, string subscriptionId);

    /// <summary>
    /// Updates the last ping sent timestamp for a subscription.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="subscriptionId">The subscription identifier.</param>
    void RecordPingSent(string connectionId, string subscriptionId);

    /// <summary>
    /// Updates the last pong received timestamp for a subscription.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="subscriptionId">The subscription identifier.</param>
    void RecordPongReceived(string connectionId, string subscriptionId);

    /// <summary>
    /// Updates the last data served timestamp for a subscription.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="subscriptionId">The subscription identifier.</param>
    void RecordDataServed(string connectionId, string subscriptionId);

    /// <summary>
    /// Removes all subscriptions for a connection.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    void RemoveConnection(string connectionId);

    /// <summary>
    /// Gets all connection health information.
    /// </summary>
    /// <returns>Collection of query connection health.</returns>
    IEnumerable<QueryConnectionHealth> GetAllConnectionHealth();

    /// <summary>
    /// Gets an observable stream of connection health updates.
    /// </summary>
    /// <returns>Observable subject emitting connection health snapshots.</returns>
    ISubject<IEnumerable<QueryConnectionHealth>> ObserveHealth();
}

/// <summary>
/// Represents an implementation of <see cref="IQueryHealthTracker"/>.
/// </summary>
[Singleton]
public sealed class QueryHealthTracker : IQueryHealthTracker
{
    readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    readonly BehaviorSubject<IEnumerable<QueryConnectionHealth>> _healthSubject = new([]);

    sealed record ConnectionInfo(string ConnectionId, string Protocol, DateTimeOffset EstablishedAt)
    {
        public ConcurrentDictionary<string, QuerySubscriptionMetadata> Subscriptions { get; } = new();
    }

    /// <inheritdoc/>
    public void RegisterSubscription(string connectionId, string protocol, QuerySubscriptionMetadata metadata)
    {
        var connection = _connections.GetOrAdd(connectionId, id => new ConnectionInfo(id, protocol, DateTimeOffset.UtcNow));
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

    void PublishUpdate()
    {
        _healthSubject.OnNext(GetAllConnectionHealth());
    }
}
