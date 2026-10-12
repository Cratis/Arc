// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle a single <see cref="EventForEventSourceId"/> as the response value.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="concurrencyScopeStrategies">The <see cref="IConcurrencyScopeStrategies"/> for resolving the expected sequence number.</param>
/// <param name="eventSources">The registered event source definitions.</param>
[method: ActivatorUtilitiesConstructor]
public class SingleEventForEventSourceIdCommandResponseValueHandler(
    IEventLog eventLog,
    IEventTypes eventTypes,
    IConcurrencyScopeStrategies concurrencyScopeStrategies,
    IEventSources eventSources) : ICommandResponseValueHandler, ICommandResponseValueHandler<EventForEventSourceId>
{
    /// <summary>
    /// Initializes a handler without validation of per-event routing against event source definitions.
    /// </summary>
    /// <param name="eventLog">The event log.</param>
    /// <param name="eventTypes">The registered event types.</param>
    /// <param name="concurrencyScopeStrategies">The concurrency scope strategies.</param>
    /// <remarks>Use the constructor taking <see cref="IEventSources"/> to validate legacy wrapper routes.</remarks>
    [Obsolete("This constructor does not validate per-event routing against event source definitions; use the constructor that takes IEventSources.")]
    public SingleEventForEventSourceIdCommandResponseValueHandler(IEventLog eventLog, IEventTypes eventTypes, IConcurrencyScopeStrategies concurrencyScopeStrategies)
        : this(eventLog, eventTypes, concurrencyScopeStrategies, null!)
    {
    }

    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is EventForEventSourceId eventForEventSourceId &&
        eventTypes.HasFor(eventForEventSourceId.Event.GetType());

    /// <inheritdoc/>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">A legacy wrapper route names a known definition and an undeclared stream on a definition-routed command.</exception>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var eventForEventSourceId = (EventForEventSourceId)value;

        // The scope belongs to the stream being written to, not to the command's own event source.
        var routing = CommandTransactionAppender.ResolveRouting(eventForEventSourceId, commandContext, eventSources);
        var concurrencyScope = (await ConcurrencyScopeBuilder.BuildFor(
            commandContext,
            concurrencyScopeStrategies.GetFor(eventLog),
            eventForEventSourceId.EventSourceId,
            routing)).Scope;
        if (!eventLog.TryEnrollForCommand(
                eventForEventSourceId.EventSourceId,
                eventForEventSourceId.Event,
                commandContext,
                concurrencyScope,
                eventForEventSourceId.SuppliedTags(),
                eventForEventSourceId.Occurred,
                routing,
                eventForEventSourceId.NamedTags))
        {
            var result = await eventLog.AppendForCommand(
                eventForEventSourceId.EventSourceId,
                eventForEventSourceId.Event,
                routing,
                concurrencyScope,
                eventForEventSourceId.SuppliedTags(),
                eventForEventSourceId.Occurred,
                CommandTransactionAppender.MergeEventTags(commandContext, eventForEventSourceId.NamedTags));

            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
