// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

/// <summary>
/// A command that returns an <see cref="IEnumerable{T}"/> of <see cref="EventForEventSourceId"/>, one per recipient.
/// </summary>
/// <param name="RecipientIds">The event source IDs of the notification recipients.</param>
/// <param name="Message">The notification message to send to each recipient.</param>
[Command]
public record BroadcastNotificationCommand(IEnumerable<EventSourceId> RecipientIds, string Message)
{
    /// <summary>
    /// Handles the command by returning a notification event for each recipient's event source.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="EventForEventSourceId"/> targeting each recipient.</returns>
    public IEnumerable<EventForEventSourceId> Handle() =>
        RecipientIds.Select(id => new EventForEventSourceId(id, new NotificationSent(Message)));
}
