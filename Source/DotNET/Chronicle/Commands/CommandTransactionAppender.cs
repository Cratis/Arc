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
    internal const string CausationEventSequenceIdProperty = "eventSequenceId";

    /// <summary>
    /// The causation type recorded for events a command appends through its transaction.
    /// </summary>
    internal static readonly CausationType CausationType = "Command";

    /// <summary>
    /// Creates the causation recorded for an event returned from a command handler.
    /// </summary>
    /// <param name="eventLog">The <see cref="IEventLog"/> the event targets.</param>
    /// <returns>The command causation for the event.</returns>
    internal static Causation CreateCommandCausation(this IEventLog eventLog) =>
        new(DateTimeOffset.Now, CausationType, new Dictionary<string, string> { { CausationEventSequenceIdProperty, eventLog.Id } });

    /// <summary>
    /// Applies the command context metadata used by returned-event handlers to an event for an explicit event source.
    /// </summary>
    /// <param name="eventLog">The <see cref="IEventLog"/> the event targets.</param>
    /// <param name="event">The event and target event source returned by the command.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> carrying the event metadata.</param>
    /// <returns>A new event value carrying the command metadata.</returns>
    internal static EventForEventSourceId WithCommandMetadata(this IEventLog eventLog, EventForEventSourceId @event, CommandContext commandContext) =>
        new(@event.EventSourceId, @event.Event, eventLog.CreateCommandCausation())
        {
            EventStreamType = commandContext.GetEventStreamType() ?? EventStreamType.All,
            EventStreamId = commandContext.GetEventStreamId() ?? EventStreamId.Default,
            EventSourceType = commandContext.GetEventSourceType() ?? EventSourceType.Default,
            Subject = commandContext.GetSubject()
        };

    /// <summary>
    /// Tries to enroll the event in the command's transaction, using the same metadata the immediate append would
    /// use from the <see cref="CommandContext"/>.
    /// </summary>
    /// <param name="eventLog">The <see cref="IEventLog"/> the event targets.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event to enroll.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> carrying the event metadata.</param>
    /// <param name="concurrencyScope">The optional <see cref="ConcurrencyScope"/> for the append.</param>
    /// <returns>True when the event was enrolled in the command's transaction; false when no transaction is active.</returns>
    internal static bool TryEnrollForCommand(this IEventLog eventLog, EventSourceId eventSourceId, object @event, CommandContext commandContext, ConcurrencyScope? concurrencyScope)
    {
        if (!CommandTransaction.TryGetActive(out var unitOfWork))
        {
            return false;
        }

        unitOfWork.AddEvent(
            eventLog.Id,
            eventSourceId,
            @event,
            eventLog.CreateCommandCausation(),
            commandContext.GetEventStreamType(),
            commandContext.GetEventStreamId(),
            commandContext.GetEventSourceType(),
            concurrencyScope,
            subject: commandContext.GetSubject());

        return true;
    }
}
