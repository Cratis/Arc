// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the payload of an <see cref="ObservableQueryHubMessageType.Subscribe"/> message sent by the client.
/// </summary>
/// <param name="QueryName">The fully qualified name of the observable query to subscribe to (e.g. <c>MyApp.Features.Authors.Listing.AllAuthors</c>).</param>
/// <param name="Arguments">Optional query-string arguments passed to the query performer.</param>
/// <param name="Page">Optional zero-based page index for paged queries.</param>
/// <param name="PageSize">Optional page size for paged queries.</param>
/// <param name="SortBy">Optional field name to sort by.</param>
/// <param name="SortDirection">Optional sort direction (<c>asc</c> or <c>desc</c>).</param>
/// <param name="TransferMode">Optional transfer mode (<c>delta</c> or <c>full</c>). When <c>delta</c>, the server sends the full snapshot on the first emission and only the change set on subsequent ones. When <c>full</c>, the server always sends the complete snapshot without a change set. Defaults to legacy behavior (snapshot plus change set) when omitted.</param>
public record ObservableQuerySubscriptionRequest(
    string QueryName,
    IDictionary<string, string?>? Arguments = null,
    int? Page = null,
    int? PageSize = null,
    string? SortBy = null,
    string? SortDirection = null,
    string? TransferMode = null);
