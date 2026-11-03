// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Describes the outcome of writing a WebSocket message, not its receipt by the client.
/// </summary>
public enum WebSocketSendStatus
{
    /// <summary>
    /// The write could not be confirmed.
    /// </summary>
    Unconfirmed = 0,

    /// <summary>
    /// No transport write was attempted.
    /// </summary>
    Skipped = 1,

    /// <summary>
    /// The complete transport write finished successfully.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// The send failed with an error.
    /// </summary>
    Failed = 3
}
