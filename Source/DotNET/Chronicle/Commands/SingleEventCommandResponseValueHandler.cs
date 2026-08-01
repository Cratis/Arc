// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle a single event as the response value.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="concurrencyScopeStrategies">The <see cref="IConcurrencyScopeStrategies"/> for resolving the expected sequence number.</param>
public class SingleEventCommandResponseValueHandler(
    IEventLog eventLog,
    IEventTypes eventTypes,
    IConcurrencyScopeStrategies concurrencyScopeStrategies) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is object obj &&
        eventTypes.HasFor(obj.GetType()) &&
        commandContext.HasEventSourceId();

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        var concurrencyScope = await ConcurrencyScopeBuilder.BuildFor(commandContext, concurrencyScopeStrategies.GetFor(eventLog), eventSourceId);
        if (!eventLog.TryEnrollForCommand(eventSourceId, value, commandContext, concurrencyScope))
        {
            var result = await eventLog.Append(
                eventSourceId,
                value,
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
