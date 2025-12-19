// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the type of WebSocket message.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WebSocketMessageType
{
    /// <summary>
    /// A data message containing query results.
    /// </summary>
    Data = 0,

    /// <summary>
    /// A ping message sent from client to server.
    /// </summary>
    Ping = 1,

    /// <summary>
    /// A pong message sent from server to client in response to a ping.
    /// </summary>
    Pong = 2
}
