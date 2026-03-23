// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Tracks the last time a message was sent to determine whether a keep-alive message should be sent.
/// </summary>
/// <remarks>
/// The keep-alive logic is transport-agnostic. Any transport that needs keep-alive behaviour should
/// create an instance of this class and call <see cref="RecordMessageSent"/> every time a message
/// is dispatched to the client. Before sending a keep-alive, check <see cref="ShouldSendKeepAlive"/>
/// to avoid sending redundant keep-alive messages when normal data is already flowing.
/// </remarks>
public class KeepAliveTracker
{
    DateTimeOffset _lastMessageSent = DateTimeOffset.UtcNow;

    /// <summary>
    /// Records that a message was sent to the client, resetting the keep-alive timer.
    /// </summary>
    public void RecordMessageSent() => _lastMessageSent = DateTimeOffset.UtcNow;

    /// <summary>
    /// Determines whether a keep-alive message should be sent given the configured interval.
    /// </summary>
    /// <param name="interval">The keep-alive interval.</param>
    /// <returns><see langword="true"/> if a keep-alive should be sent; otherwise <see langword="false"/>.</returns>
    public bool ShouldSendKeepAlive(TimeSpan interval) =>
        DateTimeOffset.UtcNow - _lastMessageSent >= interval;
}
