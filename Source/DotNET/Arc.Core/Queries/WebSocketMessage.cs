// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a WebSocket message envelope.
/// </summary>
public class WebSocketMessage
{
    /// <summary>
    /// Gets or sets the type of message.
    /// </summary>
    public WebSocketMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was sent (for ping/pong latency tracking).
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the payload data (for data messages).
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Creates a ping message.
    /// </summary>
    /// <param name="timestamp">The timestamp when the ping was sent.</param>
    /// <returns>A ping <see cref="WebSocketMessage"/>.</returns>
    public static WebSocketMessage Ping(long timestamp) => new() { Type = WebSocketMessageType.Ping, Timestamp = timestamp };

    /// <summary>
    /// Creates a pong message.
    /// </summary>
    /// <param name="timestamp">The timestamp from the original ping.</param>
    /// <returns>A pong <see cref="WebSocketMessage"/>.</returns>
    public static WebSocketMessage Pong(long timestamp) => new() { Type = WebSocketMessageType.Pong, Timestamp = timestamp };

    /// <summary>
    /// Creates a data message.
    /// </summary>
    /// <param name="data">The data payload.</param>
    /// <returns>A data <see cref="WebSocketMessage"/>.</returns>
    public static WebSocketMessage CreateData(object data) => new() { Type = WebSocketMessageType.Data, Data = data };
}
