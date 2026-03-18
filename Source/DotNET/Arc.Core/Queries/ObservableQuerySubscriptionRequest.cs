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
public record ObservableQuerySubscriptionRequest(
    string QueryName,
    IDictionary<string, string?>? Arguments = null,
    int? Page = null,
    int? PageSize = null,
    string? SortBy = null,
    string? SortDirection = null);
