// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents health information for a query connection.
/// </summary>
public sealed record QueryConnectionHealth
{
    /// <summary>
    /// Gets the connection identifier.
    /// </summary>
    public required string ConnectionId { get; init; }

    /// <summary>
    /// Gets the connection protocol (WebSocket or SSE).
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Gets when the connection was established.
    /// </summary>
    public required DateTimeOffset EstablishedAt { get; init; }

    /// <summary>
    /// Gets the subscriptions on this connection.
    /// </summary>
    public required IReadOnlyList<QuerySubscriptionMetadata> Subscriptions { get; init; }
}
