// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

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
