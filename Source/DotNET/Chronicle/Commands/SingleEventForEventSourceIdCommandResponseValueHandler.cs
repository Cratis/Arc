// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle a single <see cref="EventForEventSourceId"/> as the response value.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="concurrencyScopeStrategies">The <see cref="IConcurrencyScopeStrategies"/> for resolving the expected sequence number.</param>
public class SingleEventForEventSourceIdCommandResponseValueHandler(
    IEventLog eventLog,
    IEventTypes eventTypes,
    IConcurrencyScopeStrategies concurrencyScopeStrategies) : ICommandResponseValueHandler, ICommandResponseValueHandler<EventForEventSourceId>
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is EventForEventSourceId eventForEventSourceId &&
        eventTypes.HasFor(eventForEventSourceId.Event.GetType());

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var eventForEventSourceId = (EventForEventSourceId)value;

        // The scope belongs to the stream being written to, not to the command's own event source.
        var concurrencyScope = await ConcurrencyScopeBuilder.BuildFor(
            commandContext,
            concurrencyScopeStrategies.GetFor(eventLog),
            eventForEventSourceId.EventSourceId);
        if (!eventLog.TryEnrollForCommand(eventForEventSourceId.EventSourceId, eventForEventSourceId.Event, commandContext, concurrencyScope))
        {
            var result = await eventLog.Append(
                eventForEventSourceId.EventSourceId,
                eventForEventSourceId.Event,
                commandContext.GetEventStreamType(),
                commandContext.GetEventStreamId(),
                commandContext.GetEventSourceType(),
                correlationId: default,
                concurrencyScope: concurrencyScope,
                subject: commandContext.GetSubject());

            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
