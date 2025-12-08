// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http;

/// <summary>
/// Implementation of <see cref="IWebSocketContext"/> for <see cref="HttpListener"/>.
/// </summary>
/// <param name="listenerContext">The <see cref="HttpListenerContext"/>.</param>
public class HttpListenerWebSocketContext(HttpListenerContext listenerContext) : IWebSocketContext
{
    /// <inheritdoc/>
    public bool IsWebSocketRequest => listenerContext.Request.IsWebSocketRequest;

    /// <inheritdoc/>
    public async Task<IWebSocket> AcceptWebSocketAsync(CancellationToken cancellationToken = default)
    {
        var webSocketContext = await listenerContext.AcceptWebSocketAsync(null);
        return new HttpListenerWebSocket(webSocketContext.WebSocket);
    }
}
