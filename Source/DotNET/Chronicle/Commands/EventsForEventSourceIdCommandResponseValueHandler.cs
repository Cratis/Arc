// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle a collection containing one or more
/// <see cref="EventForEventSourceId"/> wrappers as the response value — including a mixed collection of
/// plain events and wrappers.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
/// <remarks>
/// The match is based on the runtime type of each element rather than the static type of the collection, so a
/// collection boxed as <see cref="IEnumerable{T}"/> of <see cref="object"/> — or a mixed collection of plain events
/// and <see cref="EventForEventSourceId"/> wrappers — is appended instead of falling through the handlers and being
/// silently serialized as the response payload. Each wrapper is appended to its own event source id; each plain event
/// is appended to the command's event source id.
/// </remarks>
public class EventsForEventSourceIdCommandResponseValueHandler(IEventLog eventLog, IEventTypes eventTypes) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value)
    {
        if (value is not IEnumerable enumerable || value is string)
        {
            return false;
        }

        // Validate every element in a single pass, without materializing the collection: each element must be an
        // EventForEventSourceId wrapper carrying a registered event, or a registered plain event. Short-circuit on
        // the first invalid element.
        var hasItems = false;
        var hasWrapper = false;
        foreach (var item in enumerable)
        {
            hasItems = true;
            if (item is EventForEventSourceId wrapper)
            {
                hasWrapper = true;
                if (!eventTypes.HasFor(wrapper.Event.GetType()))
                {
                    return false;
                }
            }
            else if (item is null || !eventTypes.HasFor(item.GetType()))
            {
                return false;
            }
        }

        // An empty collection statically typed as wrappers is still ours — it is recognized (and appends nothing)
        // rather than being serialized as the response payload.
        if (!hasItems)
        {
            return value is IEnumerable<EventForEventSourceId>;
        }

        // A non-empty collection must contain at least one wrapper, distinguishing it from a pure plain-event
        // collection handled by the sibling handler.
        return hasWrapper;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var items = ((IEnumerable)value).Cast<object>();
        var concurrencyScope = ConcurrencyScopeBuilder.BuildFromCommandContext(commandContext);

        foreach (var item in items)
        {
            var (eventSourceId, @event) = item is EventForEventSourceId wrapped
                ? (wrapped.EventSourceId, wrapped.Event)
                : (commandContext.GetEventSourceId(), item);

            var result = await eventLog.Append(
                eventSourceId,
                @event,
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
