// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Enrolls events a command returns from its handler into the command's transaction, so they commit atomically with
/// the command and roll back when it fails. When no command transaction is active the caller falls through to its
/// immediate append.
/// </summary>
internal static class CommandTransactionAppender
{
    /// <summary>
    /// The causation property carrying the event sequence id.
    /// </summary>
    internal const string CausationEventSequenceIdProperty = CommandCausation.EventSequenceIdProperty;

    /// <summary>
    /// The causation type recorded for events a command appends through its transaction.
    /// </summary>
    internal static readonly CausationType CausationType = CommandCausation.Type;

    /// <summary>
    /// Creates the causation recorded for an event returned from a command handler.
    /// </summary>
    /// <param name="eventLog">The <see cref="IEventLog"/> the event targets.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> of the command that produced the event.</param>
    /// <returns>The command causation for the event.</returns>
    /// <remarks>
    /// The causation names the command, not just the fact that a command was involved. An event whose causation
    /// says only "Command" can be traced back to the pipeline and no further, which is no more than the event
    /// already implies.
    /// </remarks>
    internal static Causation CreateCommandCausation(this IEventLog eventLog, CommandContext commandContext)
    {
        var properties = CommandCausation.PropertiesFor(commandContext.Type, commandContext.Command);
        properties[CausationEventSequenceIdProperty] = eventLog.Id;
        return new(DateTimeOffset.Now, CausationType, properties);
    }

    /// <summary>
    /// Applies the command context metadata used by returned-event handlers to an event for an explicit event source.
    /// </summary>
    /// <param name="eventLog">The <see cref="IEventLog"/> the event targets.</param>
    /// <param name="event">The event and target event source returned by the command.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> carrying the event metadata.</param>
    /// <returns>A new event value carrying the command metadata.</returns>
    /// <remarks>
    /// A routing value set on the wrapper (<see cref="EventForEventSourceId.EventSourceType"/>,
    /// <see cref="EventForEventSourceId.EventStreamType"/>, <see cref="EventForEventSourceId.EventStreamId"/>,
    /// <see cref="EventForEventSourceId.Subject"/>) wins over the command context. A value is set when it differs
    /// from its sentinel (<c>Default</c>, <c>All</c>, <c>Default</c>, and not null); otherwise the command context
    /// value is used.
    /// <para>
    /// The wrapper's own <see cref="EventForEventSourceId.Occurred"/> and <see cref="EventForEventSourceId.Tags"/> are
    /// kept: they are values the command supplied for this particular event, and dropping them would silently record
    /// the append time and no tags instead.
    /// </para>
    /// </remarks>
    internal static EventForEventSourceId WithCommandMetadata(this IEventLog eventLog, EventForEventSourceId @event, CommandContext commandContext) =>
        WithRouting(eventLog, @event, commandContext, ResolveRouting(@event, commandContext));

    /// <summary>
    /// Resolves the routing for an event: a value set on the wrapper wins, otherwise the command context value is used.
    /// </summary>
    /// <param name="event">The wrapper the command returned, or null for a plain event that has no routing of its own.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> carrying the fallback event metadata.</param>
    /// <returns>The resolved <see cref="EventRouting"/>.</returns>
    internal static EventRouting ResolveRouting(EventForEventSourceId? @event, CommandContext commandContext)
    {
        var (eventSource, eventStream) = ResolveDefinition(@event, commandContext);

        return new(
            @event is not null && @event.EventStreamType != EventStreamType.All ? @event.EventStreamType : commandContext.GetEventStreamType(),
            @event is not null && @event.EventStreamId != EventStreamId.Default ? @event.EventStreamId : commandContext.GetEventStreamId(),
            @event is not null && @event.EventSourceType != EventSourceType.Default ? @event.EventSourceType : commandContext.GetEventSourceType(),
            @event?.Subject ?? commandContext.GetSubject(),
            eventSource,
            eventStream);
    }

    /// <summary>
    /// Resolves the event source definition and stream an event is appended through.
    /// </summary>
    /// <param name="event">The wrapper the command returned, or null for a plain event.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> carrying the fallback definition.</param>
    /// <returns>The definition type and stream name; the definition is null when the event is appended by string routing.</returns>
    /// <remarks>
    /// The definition and its stream are one routing decision, so the wrapper replaces them together. A wrapper naming
    /// another definition brings its own stream and inherits none of the command's. A wrapper that only names a stream
    /// uses the command's definition. A wrapper that routes with the legacy source or stream type strings leaves the
    /// definition out entirely: appending through the command's definition would silently discard those strings.
    /// </remarks>
    internal static (Type? EventSource, string? EventStream) ResolveDefinition(EventForEventSourceId? @event, CommandContext commandContext)
    {
        if (@event?.EventSource is not null)
        {
            return (@event.EventSource, @event.EventStream);
        }

        if (@event is not null && (@event.EventSourceType != EventSourceType.Default || @event.EventStreamType != EventStreamType.All))
        {
            return (null, null);
        }

        return (commandContext.GetEventSource(), @event?.EventStream ?? commandContext.GetEventStream());
    }

    /// <summary>
    /// Gets the tags the command supplied on an event wrapper, or null when it supplied none.
    /// </summary>
    /// <param name="event">The event and target event source returned by the command.</param>
    /// <returns>The supplied tags, or null when the wrapper carries none.</returns>
    /// <remarks>
    /// A wrapper's tags default to an empty collection. Chronicle treats empty and null alike, so an untagged
    /// wrapper appends exactly as it did before wrapper tags were forwarded.
    /// </remarks>
    internal static IEnumerable<string>? SuppliedTags(this EventForEventSourceId @event) =>
        @event.Tags is { } tags && tags.Any() ? tags : null;

    /// <summary>
    /// Tries to enroll the event in the command's transaction, using the same metadata the immediate append would
    /// use from the <see cref="CommandContext"/>.
    /// </summary>
    /// <param name="eventLog">The <see cref="IEventLog"/> the event targets.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event to enroll.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> carrying the event metadata.</param>
    /// <param name="concurrencyScope">The optional <see cref="ConcurrencyScope"/> for the append.</param>
    /// <param name="tags">Optional tags the command supplied for this event.</param>
    /// <param name="occurred">Optional occurrence time the command supplied for this event.</param>
    /// <param name="routing">Optional resolved routing; defaults to the command context.</param>
    /// <returns>True when the event was enrolled in the command's transaction; false when no transaction is active.</returns>
    internal static bool TryEnrollForCommand(
        this IEventLog eventLog,
        EventSourceId eventSourceId,
        object @event,
        CommandContext commandContext,
        ConcurrencyScope? concurrencyScope,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        EventRouting? routing = default)
    {
        routing ??= ResolveRouting(null, commandContext);
        if (!CommandTransaction.TryGetActive(out var unitOfWork))
        {
            CommandTransaction.RefuseImmediateAppend();
            return false;
        }

        if (routing.EventSource is not null)
        {
            var eventForEventSource = new EventForEventSourceId(eventSourceId, @event, eventLog.CreateCommandCausation(commandContext))
            {
                EventSource = routing.EventSource,
                EventStream = routing.EventStream,
                EventStreamId = routing.EventStreamId ?? EventStreamId.Default,
                Subject = routing.Subject,
                Occurred = occurred,
                Tags = tags ?? []
            };
            unitOfWork.AddEvents(
                eventLog.Id,
                [eventForEventSource],
                [new(eventSourceId, concurrencyScope ?? ConcurrencyScope.NotSet)]);
        }
        else
        {
            unitOfWork.AddEvent(
                eventLog.Id,
                eventSourceId,
                @event,
                eventLog.CreateCommandCausation(commandContext),
                routing.EventStreamType,
                routing.EventStreamId,
                routing.EventSourceType,
                concurrencyScope,
                tags,
                occurred,
                routing.Subject);
        }

        return true;
    }

    /// <summary>
    /// Appends an event using definition routing when a command declares it, otherwise using legacy routing.
    /// </summary>
    /// <param name="eventLog">The event log to append to.</param>
    /// <param name="eventSourceId">The event source instance id.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="routing">The resolved routing metadata.</param>
    /// <param name="concurrencyScope">The optional concurrency scope.</param>
    /// <param name="tags">The optional tags.</param>
    /// <param name="occurred">The optional occurrence time.</param>
    /// <returns>The append result.</returns>
    internal static Task<AppendResult> AppendForCommand(
        this IEventLog eventLog,
        EventSourceId eventSourceId,
        object @event,
        EventRouting routing,
        ConcurrencyScope? concurrencyScope,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default) =>
        routing.EventSource is not null
            ? eventLog.Append(routing.EventSource, eventSourceId, @event, routing.EventStream, routing.EventStreamId, correlationId: default, tags, concurrencyScope, occurred, routing.Subject)
            : eventLog.Append(eventSourceId, @event, routing.EventStreamType, routing.EventStreamId, routing.EventSourceType, correlationId: default, tags, concurrencyScope, occurred, routing.Subject);

    static EventForEventSourceId WithRouting(IEventLog eventLog, EventForEventSourceId @event, CommandContext commandContext, EventRouting routing) =>
        new(@event.EventSourceId, @event.Event, eventLog.CreateCommandCausation(commandContext))
        {
            EventStreamType = routing.EventStreamType ?? EventStreamType.All,
            EventStreamId = routing.EventStreamId ?? EventStreamId.Default,
            EventSourceType = routing.EventSourceType ?? EventSourceType.Default,
            Subject = routing.Subject,
            EventSource = routing.EventSource,
            EventStream = routing.EventStream,
            Occurred = @event.Occurred,
            Tags = @event.Tags
        };
}
