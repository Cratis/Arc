// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle an <see cref="IEnumerable{T}"/> of <see cref="EventForEventSourceId"/> as the response value.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
public class EventsForEventSourceIdCommandResponseValueHandler(IEventLog eventLog, IEventTypes eventTypes) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is IEnumerable<EventForEventSourceId> events &&
        events.All(e => eventTypes.HasFor(e.Event.GetType()));

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var events = (IEnumerable<EventForEventSourceId>)value;
        var concurrencyScope = ConcurrencyScopeBuilder.BuildFromCommandContext(commandContext);

        foreach (var eventForEventSourceId in events)
        {
            var result = await eventLog.Append(
                eventForEventSourceId.EventSourceId,
                eventForEventSourceId.Event,
                commandContext.GetEventStreamType(),
                commandContext.GetEventStreamId(),
                commandContext.GetEventSourceType(),
                correlationId: default,
                concurrencyScope: concurrencyScope);

            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
