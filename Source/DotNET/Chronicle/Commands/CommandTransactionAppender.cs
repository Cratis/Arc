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
            new Causation(DateTimeOffset.Now, CausationType, new Dictionary<string, string> { { CausationEventSequenceIdProperty, eventLog.Id } }),
            commandContext.GetEventStreamType(),
            commandContext.GetEventStreamId(),
            commandContext.GetEventSourceType(),
            concurrencyScope,
            subject: commandContext.GetSubject());

        return true;
    }
}
