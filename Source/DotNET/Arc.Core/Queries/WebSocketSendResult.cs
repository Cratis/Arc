// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a WebSocket write outcome and its optional failure.
/// </summary>
public sealed class WebSocketSendResult
{
    WebSocketSendResult(WebSocketSendStatus status, Exception? error = null)
    {
        Status = status;
        Error = error;
    }

    /// <summary>
    /// Gets an unconfirmed write result.
    /// </summary>
    public static WebSocketSendResult Unconfirmed { get; } = new(WebSocketSendStatus.Unconfirmed);

    /// <summary>
    /// Gets a skipped write result.
    /// </summary>
    public static WebSocketSendResult Skipped { get; } = new(WebSocketSendStatus.Skipped);

    /// <summary>
    /// Gets a completed write result.
    /// </summary>
    public static WebSocketSendResult Sent { get; } = new(WebSocketSendStatus.Sent);

    /// <summary>
    /// Gets the write status.
    /// </summary>
    public WebSocketSendStatus Status { get; }

    /// <summary>
    /// Gets the failure, only for a failed send.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Creates a failed send result.
    /// </summary>
    /// <param name="error">The send failure.</param>
    /// <returns>The failed result.</returns>
    /// <exception cref="ArgumentNullException">The error is null.</exception>
    public static WebSocketSendResult Failed(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(WebSocketSendStatus.Failed, error);
    }
}
