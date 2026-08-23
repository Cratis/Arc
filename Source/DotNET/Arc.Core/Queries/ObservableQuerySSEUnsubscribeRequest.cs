// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the POST body for unsubscribing from a query over the SSE hub transport.
/// </summary>
/// <param name="ConnectionId">The server-assigned SSE connection identifier.</param>
/// <param name="QueryId">The identifier of the subscription to cancel.</param>
/// <param name="Revision">The optional exact revision to tombstone.</param>
[method: JsonConstructor]
public record ObservableQuerySSEUnsubscribeRequest(
    string ConnectionId,
    string QueryId,
    long? Revision = null)
{
    /// <summary>
    /// Initializes a legacy unsubscribe without a revision.
    /// </summary>
    /// <param name="connectionId">The server-assigned SSE connection identifier.</param>
    /// <param name="queryId">The client-assigned query identifier.</param>
    public ObservableQuerySSEUnsubscribeRequest(string connectionId, string queryId)
        : this(connectionId, queryId, null)
    {
    }

    /// <summary>
    /// Deconstructs the original two-value contract.
    /// </summary>
    /// <param name="connectionId">The server-assigned SSE connection identifier.</param>
    /// <param name="queryId">The client-assigned query identifier.</param>
    public void Deconstruct(out string connectionId, out string queryId)
    {
        connectionId = ConnectionId;
        queryId = QueryId;
    }
}
