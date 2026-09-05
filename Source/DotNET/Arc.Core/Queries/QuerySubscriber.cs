// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a single active subscriber for a query, combining connection and subscription data.
/// </summary>
public sealed record QuerySubscriber
{
    /// <summary>
    /// Gets the connection identifier (e.g. <c>ws-1</c> for WebSocket, a GUID for SSE).
    /// </summary>
    public required string ConnectionId { get; init; }

    /// <summary>
    /// Gets the transport protocol (<c>WebSocket</c> or <c>SSE</c>).
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Gets the subscription identifier — the client-generated query ID.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// Gets when this subscription was first established.
    /// </summary>
    public required DateTimeOffset ConnectedAt { get; init; }

    /// <summary>
    /// Gets the last time a ping was sent to this subscriber.
    /// </summary>
    public DateTimeOffset? LastPingSentAt { get; init; }

    /// <summary>
    /// Gets the last time a pong was received from this subscriber.
    /// </summary>
    public DateTimeOffset? LastPongReceivedAt { get; init; }

    /// <summary>
    /// Gets the last time data was served to this subscriber.
    /// </summary>
    public DateTimeOffset? LastDataServedAt { get; init; }

    /// <summary>
    /// Gets client identity and transport information for this subscriber.
    /// </summary>
    public required QuerySubscriptionClientInfo ClientInfo { get; init; }
}
