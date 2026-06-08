// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents metadata for a single query subscription.
/// </summary>
public sealed record QuerySubscriptionMetadata
{
    /// <summary>
    /// Gets the unique identifier for this subscription (client-generated queryId).
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// Gets the query identifier (fully qualified query name).
    /// </summary>
    public required string QueryIdentifier { get; init; }

    /// <summary>
    /// Gets the read model type.
    /// </summary>
    public required string ReadModelType { get; init; }

    /// <summary>
    /// Gets when the subscription was first connected.
    /// </summary>
    public required DateTimeOffset ConnectedAt { get; init; }

    /// <summary>
    /// Gets the client information (headers, user agent, etc.).
    /// </summary>
    public required QuerySubscriptionClientInfo ClientInfo { get; init; }

    /// <summary>
    /// Gets the last time a ping was sent to the client.
    /// </summary>
    public DateTimeOffset? LastPingSentAt { get; set; }

    /// <summary>
    /// Gets the last time a pong was received from the client.
    /// </summary>
    public DateTimeOffset? LastPongReceivedAt { get; set; }

    /// <summary>
    /// Gets the last time data was served to the client.
    /// </summary>
    public DateTimeOffset? LastDataServedAt { get; set; }
}

/// <summary>
/// Represents client information for a query subscription.
/// </summary>
public sealed record QuerySubscriptionClientInfo
{
    /// <summary>
    /// Gets the remote IP address.
    /// </summary>
    public string? RemoteIpAddress { get; init; }

    /// <summary>
    /// Gets the user agent string.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Gets the user identity (if authenticated).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the connection protocol (WebSocket or SSE).
    /// </summary>
    public required string Protocol { get; init; }
}
