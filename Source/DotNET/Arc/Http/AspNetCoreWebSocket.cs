// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Cratis.Arc.Http;

namespace Cratis.Arc.AspNetCore.Http;

/// <summary>
/// Implementation of <see cref="IWebSocket"/> for ASP.NET Core WebSockets.
/// </summary>
/// <param name="webSocket">The underlying <see cref="WebSocket"/>.</param>
public class AspNetCoreWebSocket(WebSocket webSocket) : IWebSocket
{
    /// <inheritdoc/>
    public WebSocketState State => webSocket.State;

    /// <inheritdoc/>
    public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        return webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        return webSocket.ReceiveAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        webSocket.Dispose();
    }
}
