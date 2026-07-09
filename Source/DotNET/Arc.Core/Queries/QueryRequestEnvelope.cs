// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the JSON body of an HTTP QUERY request (RFC 10008) — the arguments, paging and sorting
/// that a GET request would otherwise carry in the query string.
/// </summary>
public class QueryRequestEnvelope
{
    /// <summary>
    /// Gets the query arguments keyed by parameter name.
    /// </summary>
    public IDictionary<string, JsonElement>? Arguments { get; init; }

    /// <summary>
    /// Gets the paging for the query, if any.
    /// </summary>
    public PagingRequest? Paging { get; init; }

    /// <summary>
    /// Gets the sorting for the query, if any.
    /// </summary>
    public SortingRequest? Sorting { get; init; }

    /// <summary>
    /// Represents paging in a <see cref="QueryRequestEnvelope"/>.
    /// </summary>
    /// <param name="Page">The zero-based page number.</param>
    /// <param name="PageSize">The number of items per page.</param>
    public record PagingRequest(int Page, int PageSize);

    /// <summary>
    /// Represents sorting in a <see cref="QueryRequestEnvelope"/>.
    /// </summary>
    /// <param name="Field">The field to sort by.</param>
    /// <param name="Direction">The sort direction (<c>asc</c> or <c>desc</c>).</param>
    public record SortingRequest(string Field, string Direction);
}
