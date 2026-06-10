// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

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
