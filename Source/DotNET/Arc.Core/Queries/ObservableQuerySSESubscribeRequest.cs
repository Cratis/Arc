// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the POST body for subscribing to a query over the SSE hub transport.
/// The <see cref="ConnectionId"/> correlates this request with an established SSE stream.
/// </summary>
/// <param name="ConnectionId">The server-assigned SSE connection identifier received in the <see cref="ObservableQueryHubMessageType.Connected"/> message.</param>
/// <param name="QueryId">The client-assigned identifier for this subscription.</param>
/// <param name="Request">The subscription details (query name, arguments, paging, sorting).</param>
/// <param name="Revision">The optional positive client revision used to order operations for a query id.</param>
[method: JsonConstructor]
public record ObservableQuerySSESubscribeRequest(
    string ConnectionId,
    string QueryId,
    ObservableQuerySubscriptionRequest Request,
    long? Revision = null)
{
    /// <summary>
    /// Initializes a new instance without a revision, preserving the original public constructor.
    /// </summary>
    /// <param name="connectionId">The server-assigned SSE connection identifier.</param>
    /// <param name="queryId">The client-assigned query identifier.</param>
    /// <param name="request">The subscription details.</param>
    public ObservableQuerySSESubscribeRequest(
        string connectionId,
        string queryId,
        ObservableQuerySubscriptionRequest request)
        : this(connectionId, queryId, request, null)
    {
    }

    /// <summary>
    /// Deconstructs the original three-value contract.
    /// </summary>
    /// <param name="connectionId">The server-assigned SSE connection identifier.</param>
    /// <param name="queryId">The client-assigned query identifier.</param>
    /// <param name="request">The subscription details.</param>
    public void Deconstruct(
        out string connectionId,
        out string queryId,
        out ObservableQuerySubscriptionRequest request)
    {
        connectionId = ConnectionId;
        queryId = QueryId;
        request = Request;
    }
}
