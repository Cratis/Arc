// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the types of messages exchanged in the <see cref="ObservableQueryHub"/> protocol.
/// </summary>
public enum ObservableQueryHubMessageType
{
    /// <summary>
    /// Client subscribes to an observable query.
    /// </summary>
    Subscribe = 0,

    /// <summary>
    /// Client unsubscribes from an observable query.
    /// </summary>
    Unsubscribe = 1,

    /// <summary>
    /// Server pushes a query result update to the client.
    /// </summary>
    QueryResult = 2,

    /// <summary>
    /// Server notifies the client that the requested query is not authorized.
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// Server notifies the client that an error occurred for a specific query subscription.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Keep-alive ping message.
    /// </summary>
    Ping = 5,

    /// <summary>
    /// Response to a ping message.
    /// </summary>
    Pong = 6,
}
