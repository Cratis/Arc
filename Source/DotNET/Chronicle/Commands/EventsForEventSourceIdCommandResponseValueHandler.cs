// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle a collection containing one or more
/// <see cref="EventForEventSourceId"/> wrappers as the response value — including a mixed collection of
/// plain events and wrappers.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="concurrencyScopeStrategies">The <see cref="IConcurrencyScopeStrategies"/> for resolving the expected sequence number.</param>
/// <remarks>
/// The match is based on the runtime type of each element rather than the static type of the collection, so a
/// collection boxed as <see cref="IEnumerable{T}"/> of <see cref="object"/> — or a mixed collection of plain events
/// and <see cref="EventForEventSourceId"/> wrappers — is appended instead of falling through the handlers and being
/// silently serialized as the response payload. Each wrapper is appended to its own event source id; each plain event
/// is appended to the command's event source id.
/// </remarks>
public class EventsForEventSourceIdCommandResponseValueHandler(
    IEventLog eventLog,
    IEventTypes eventTypes,
    IConcurrencyScopeStrategies concurrencyScopeStrategies) : ICommandResponseValueHandler, ICommandResponseValueHandler<IEnumerable<EventForEventSourceId>>
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
        var strategy = concurrencyScopeStrategies.GetFor(eventLog);

        // Resolve every event's routing and guard first. Events for one event source id share one scope, so guards that
        // cannot be shared are rejected here, before the first event is enrolled or appended.
        var targets = new List<(EventSourceId EventSourceId, object Event, IEnumerable<string>? Tags, DateTimeOffset? Occurred, EventRouting Routing, IEnumerable<NamedTag> NamedTags)>();
        var derivedGuards = new List<(EventSourceId EventSourceId, DerivedConcurrencyScope Derived)>();
        foreach (var item in ((IEnumerable)value).Cast<object>())
        {
            // A wrapper keeps its own tags and occurrence time; a plain event has none of its own.
            var wrapper = item as EventForEventSourceId;
            var (eventSourceId, @event, tags, occurred) = wrapper is not null
                ? (wrapper.EventSourceId, wrapper.Event, wrapper.SuppliedTags(), wrapper.Occurred)
                : (commandContext.GetEventSourceId(), item, null, null);
            var routing = CommandTransactionAppender.ResolveRouting(wrapper, commandContext);
            targets.Add((eventSourceId, @event, tags, occurred, routing, CommandTransactionAppender.MergeEventTags(commandContext, wrapper?.NamedTags)));

            // The caller's own choice is resolved once per id, however many events it takes.
            var derived = derivedGuards.Find(_ => _.EventSourceId == eventSourceId && _.Derived.Origin == DerivedConcurrencyScopeOrigin.Explicit).Derived
                ?? await ConcurrencyScopeBuilder.BuildFor(commandContext, strategy, eventSourceId, routing);
            derivedGuards.Add((eventSourceId, derived));
        }

        // A scope carries the expected tail of one stream, so it cannot be shared across the streams a
        // cross-stream command writes to - each target gets its own, resolved once however many events it takes.
        var concurrencyScopesByEventSourceId = CommandConcurrencyScopes.Resolve(derivedGuards);

        foreach (var (eventSourceId, @event, tags, occurred, routing, namedTags) in targets)
        {
            var concurrencyScope = concurrencyScopesByEventSourceId[eventSourceId];

            if (eventLog.TryEnrollForCommand(eventSourceId, @event, commandContext, concurrencyScope, tags, occurred, routing, namedTags))
            {
                continue;
            }

            var result = await eventLog.AppendForCommand(
                eventSourceId,
                @event,
                routing,
                concurrencyScope,
                tags,
                occurred,
                namedTags);

            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
