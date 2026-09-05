// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle multiple events as the response value.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="concurrencyScopeStrategies">The <see cref="IConcurrencyScopeStrategies"/> for resolving the expected sequence number.</param>
/// <remarks>
/// This handler intentionally has no typed response declaration. Its event-type registry decides at runtime whether
/// an arbitrary collection contains domain events; declaring <c>IEnumerable&lt;object&gt;</c> would incorrectly hide
/// ordinary client response collections from generated proxies.
/// </remarks>
public class EventsCommandResponseValueHandler(
    IEventLog eventLog,
    IEventTypes eventTypes,
    IConcurrencyScopeStrategies concurrencyScopeStrategies) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        (value is IEnumerable<object> objects) &&
        objects.All(o => o is not null && eventTypes.HasFor(o.GetType())) &&
        commandContext.HasEventSourceId();

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        var events = (IEnumerable<object>)value;
        if (events.Any())
        {
            var concurrencyScope = await ConcurrencyScopeBuilder.BuildFor(commandContext, concurrencyScopeStrategies.GetFor(eventLog), eventSourceId);
            var subject = commandContext.GetSubject();

            if (CommandTransaction.TryGetActive(out _))
            {
                foreach (var @event in events)
                {
                    eventLog.TryEnrollForCommand(eventSourceId, @event, commandContext, concurrencyScope);
                }
            }
            else if (subject is not null)
            {
                foreach (var @event in events)
                {
                    var appendResult = await eventLog.Append(
                        eventSourceId,
                        @event,
                        commandContext.GetEventStreamType(),
                        commandContext.GetEventStreamId(),
                        commandContext.GetEventSourceType(),
                        correlationId: default,
                        concurrencyScope: concurrencyScope,
                        subject: subject);

                    if (!appendResult.IsSuccess)
                    {
                        return appendResult.ToCommandResult();
                    }
                }
            }
            else
            {
                var result = await eventLog.AppendMany(
                    eventSourceId,
                    events,
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
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
