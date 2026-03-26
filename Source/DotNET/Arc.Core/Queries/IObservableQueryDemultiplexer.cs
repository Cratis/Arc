// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the observable query demultiplexer — a composite endpoint that accepts multiplexed observable
/// query subscriptions over a single persistent connection and routes them to individual query pipelines.
/// </summary>
/// <remarks>
/// <para>
/// Three fixed endpoints are provided under <c>/.cratis/queries</c>:
/// <list type="bullet">
///   <item><description><c>/.cratis/queries/ws</c> — WebSocket; bidirectional subscribe/unsubscribe.</description></item>
///   <item><description><c>/.cratis/queries/sse</c> — Server-Sent Events; multiplexed stream identified by a server-assigned connection ID.</description></item>
///   <item><description><c>/.cratis/queries/sse/subscribe</c> and <c>/.cratis/queries/sse/unsubscribe</c> — POST endpoints for managing SSE subscriptions.</description></item>
/// </list>
/// </para>
/// <para>
/// Both transports honor the query authorization pipeline; unauthorized subscriptions receive an
/// <see cref="ObservableQueryHubMessageType.Unauthorized"/> response rather than data.
/// </para>
/// </remarks>
public interface IObservableQueryDemultiplexer
{
    /// <summary>
    /// Handles an incoming WebSocket connection on the <c>/.cratis/queries/ws</c> endpoint.
    /// </summary>
    /// <remarks>
    /// The method reads <see cref="ObservableQueryHubMessage"/> frames from the socket. Clients
    /// subscribe to individual queries by sending <see cref="ObservableQueryHubMessageType.Subscribe"/>
    /// messages and can unsubscribe at any time using <see cref="ObservableQueryHubMessageType.Unsubscribe"/>.
    /// Ping/pong keep-alive frames are also supported.
    /// </remarks>
    /// <param name="context">The <see cref="IHttpRequestContext"/> for the request.</param>
    /// <returns>A <see cref="Task"/> that completes when the WebSocket connection closes.</returns>
    Task HandleWebSocketConnection(IHttpRequestContext context);

    /// <summary>
    /// Handles an incoming SSE connection on the <c>/.cratis/queries/sse</c> endpoint.
    /// </summary>
    /// <remarks>
    /// Establishes a persistent SSE stream. The server sends a <see cref="ObservableQueryHubMessageType.Connected"/>
    /// message containing the connection identifier. The client then uses the POST endpoints to subscribe/unsubscribe
    /// using this identifier. The stream carries all subscription results and keep-alive pings.
    /// </remarks>
    /// <param name="context">The <see cref="IHttpRequestContext"/> for the request.</param>
    /// <returns>A <see cref="Task"/> that completes when the client disconnects.</returns>
    Task HandleSSEConnection(IHttpRequestContext context);

    /// <summary>
    /// Handles a POST request to subscribe to a query over an established SSE connection.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> for the request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleSSESubscribe(IHttpRequestContext context);

    /// <summary>
    /// Handles a POST request to unsubscribe from a query over an established SSE connection.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> for the request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleSSEUnsubscribe(IHttpRequestContext context);
}
