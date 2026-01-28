// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Represents an abstraction for WebSocket operations.
/// </summary>
public interface IWebSocketContext
{
    /// <summary>
    /// Gets a value indicating whether the request is a WebSocket request.
    /// </summary>
    bool IsWebSocketRequest { get; }

    /// <summary>
    /// Accepts the WebSocket connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="IWebSocket"/> for communication.</returns>
    Task<IWebSocket> AcceptWebSocket(CancellationToken cancellationToken = default);
}
