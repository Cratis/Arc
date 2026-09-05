// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Tracks the last time a message was sent to determine whether a keep-alive message should be sent.
/// </summary>
/// <remarks>
/// The keep-alive logic is transport-agnostic. Any transport that needs keep-alive behavior should
/// create an instance of this class and call <see cref="RecordMessageSent"/> every time a message
/// is dispatched to the client. Before sending a keep-alive, check <see cref="ShouldSendKeepAlive"/>
/// to avoid sending redundant keep-alive messages when normal data is already flowing.
/// </remarks>
public class KeepAliveTracker
{
    DateTimeOffset _lastMessageSent;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeepAliveTracker"/> class.
    /// </summary>
    public KeepAliveTracker() => _lastMessageSent = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeepAliveTracker"/> class with a specific initial timestamp.
    /// </summary>
    /// <param name="initialLastMessageSent">The initial value for the last-message-sent timestamp.</param>
    public KeepAliveTracker(DateTimeOffset initialLastMessageSent) =>
        _lastMessageSent = initialLastMessageSent;

    /// <summary>
    /// Records that a message was sent to the client, resetting the keep-alive timer.
    /// </summary>
    public void RecordMessageSent() => RecordActivity();

    /// <summary>
    /// Records any connection activity (message sent or received), resetting the keep-alive timer.
    /// No keep-alive message is needed while the connection is actively exchanging data.
    /// </summary>
    public void RecordActivity() => _lastMessageSent = DateTimeOffset.UtcNow;

    /// <summary>
    /// Determines whether a keep-alive message should be sent given the configured interval.
    /// </summary>
    /// <param name="interval">The keep-alive interval.</param>
    /// <returns><see langword="true"/> if a keep-alive should be sent; otherwise <see langword="false"/>.</returns>
    public bool ShouldSendKeepAlive(TimeSpan interval) =>
        DateTimeOffset.UtcNow - _lastMessageSent >= interval;

    /// <summary>
    /// Gets how long remains until a keep-alive message is due, measured from the last message sent.
    /// </summary>
    /// <param name="interval">The keep-alive interval.</param>
    /// <returns>The remaining time, or <see cref="TimeSpan.Zero"/> when a keep-alive is already due.</returns>
    /// <remarks>
    /// Keep-alive loops should wait for exactly this long rather than for a fixed interval. Waiting a
    /// fixed interval schedules checks on a grid that is independent of when messages actually go out,
    /// so a data message landing mid-interval defers the next keep-alive to the following tick — allowing
    /// gaps of up to twice the interval. Clients that treat silence as a dead connection then disconnect
    /// while the server still considers the connection healthy.
    /// </remarks>
    public TimeSpan GetTimeUntilNextKeepAlive(TimeSpan interval)
    {
        var remaining = _lastMessageSent + interval - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
