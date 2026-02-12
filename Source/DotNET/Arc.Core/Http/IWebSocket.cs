// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Arc.Http;

/// <summary>
/// Represents an abstraction for WebSocket communication.
/// </summary>
public interface IWebSocket : IDisposable
{
    /// <summary>
    /// Gets the current state of the WebSocket connection.
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    /// Sends data over the WebSocket connection.
    /// </summary>
    /// <param name="buffer">The data to send.</param>
    /// <param name="messageType">The type of message.</param>
    /// <param name="endOfMessage">Whether this is the end of the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task Send(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Receives data from the WebSocket connection.
    /// </summary>
    /// <param name="buffer">The buffer to receive data into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the receive operation.</returns>
    Task<WebSocketReceiveResult> Receive(ArraySegment<byte> buffer, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the WebSocket connection.
    /// </summary>
    /// <param name="closeStatus">The close status.</param>
    /// <param name="statusDescription">The status description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task Close(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
}
