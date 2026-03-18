// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the observable query hub — a composite endpoint that multiplexes multiple observable query
/// subscriptions over a single persistent connection.
/// </summary>
/// <remarks>
/// <para>
/// Two fixed endpoints are provided under <c>/.cratis/queries</c>:
/// <list type="bullet">
///   <item><description><c>/.cratis/queries/ws</c> — WebSocket; bidirectional subscribe/unsubscribe.</description></item>
///   <item><description><c>/.cratis/queries/sse</c> — Server-Sent Events; one query per connection specified via query parameters.</description></item>
/// </list>
/// </para>
/// <para>
/// Both transports honour the query authorization pipeline; unauthorized subscriptions receive an
/// <see cref="ObservableQueryHubMessageType.Unauthorized"/> response rather than data.
/// </para>
/// </remarks>
public interface IObservableQueryHub
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
    /// The query to subscribe to is identified by the <c>query</c> query-string parameter (the fully
    /// qualified query name). Additional query-string parameters are forwarded as query arguments.
    /// The server streams <c>data:</c> frames containing serialised <see cref="ObservableQueryHubMessage"/>
    /// until the client disconnects.
    /// </remarks>
    /// <param name="context">The <see cref="IHttpRequestContext"/> for the request.</param>
    /// <returns>A <see cref="Task"/> that completes when the client disconnects.</returns>
    Task HandleSSEConnection(IHttpRequestContext context);
}
