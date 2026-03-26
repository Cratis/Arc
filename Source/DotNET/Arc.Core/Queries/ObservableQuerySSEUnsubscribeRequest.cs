// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the POST body for unsubscribing from a query over the SSE hub transport.
/// </summary>
/// <param name="ConnectionId">The server-assigned SSE connection identifier.</param>
/// <param name="QueryId">The identifier of the subscription to cancel.</param>
public record ObservableQuerySSEUnsubscribeRequest(
    string ConnectionId,
    string QueryId);
