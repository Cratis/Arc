// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a query-centric view of all active subscriptions for a single query.
/// </summary>
/// <remarks>
/// <see cref="QueryName"/> matches the <c>queryName</c> property of the generated TypeScript proxy
/// exactly, enabling cross-stack correlation between frontend cache diagnostics and backend
/// subscription state regardless of transport mode (multiplexed WebSocket or direct SSE).
/// </remarks>
public sealed record QuerySubscriptionAggregate
{
    /// <summary>
    /// Gets the fully qualified query name (e.g. <c>MyApp.Authors.Listing.AllAuthors</c>).
    /// Matches the <c>queryName</c> property on the generated TypeScript proxy.
    /// </summary>
    public required string QueryName { get; init; }

    /// <summary>
    /// Gets the total number of active subscribers for this query.
    /// </summary>
    public int TotalSubscriptions => Subscribers.Count;

    /// <summary>
    /// Gets all active subscribers for this query, one entry per physical connection/subscription pair.
    /// </summary>
    public required IReadOnlyList<QuerySubscriber> Subscribers { get; init; }
}
