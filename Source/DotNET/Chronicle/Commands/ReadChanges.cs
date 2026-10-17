// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Describes a decision read that changed before commit as a validation result.
/// </summary>
internal static class ReadChanges
{
    /// <summary>
    /// The reason detail of a guarded read that changed before commit.
    /// </summary>
    internal const string ReasonDetail = "ReadChanged";

    /// <summary>
    /// Creates the validation result for a decision conflict.
    /// </summary>
    /// <param name="conflict">The <see cref="DecisionConflict"/> Chronicle reported.</param>
    /// <param name="commandType">The command that read the model.</param>
    /// <param name="enforcing">Whether read protection was enforced for the command.</param>
    /// <param name="services">The command's services, used to name the conflicting event type, if available.</param>
    /// <returns>The <see cref="ValidationResult"/> describing the change.</returns>
    /// <remarks>
    /// When Chronicle reports the first event that matched the guard after the read, the message and the state name it.
    /// Otherwise they carry only the read model and its key.
    /// </remarks>
    internal static ValidationResult For(DecisionConflict conflict, Type commandType, bool enforcing, IServiceProvider? services)
    {
        var eventType = conflict.ConflictingEventTypeId is { } eventTypeId ? NameOf(eventTypeId, services) : null;
        var cause = Cause(eventType, conflict.ConflictingEventSequenceNumber);
        var message = enforcing
            ? $"{conflict.ReadModelType.Name} '{conflict.Key}' changed after {commandType.Name} read it{cause}. Nothing from this command was written; submit it again."
            : $"{conflict.ReadModelType.Name} '{conflict.Key}' changed after it was read{cause}. Read it again and resubmit.";
        object state = eventType is null && conflict.ConflictingEventSequenceNumber is null
            ? new { readModel = conflict.ReadModelType.Name, key = (string)conflict.Key }
            : new
            {
                readModel = conflict.ReadModelType.Name,
                key = (string)conflict.Key,
                conflictingEventTypeId = conflict.ConflictingEventTypeId?.Value,
                conflictingEventType = eventType,
                conflictingEventSequenceNumber = conflict.ConflictingEventSequenceNumber?.Value
            };

        return ValidationResult.Error(
            message,
            state: state,
            reason: ValidationResultReason.ConcurrencyViolation,
            reasonDetail: enforcing ? ReasonDetail : null);
    }

    static string Cause(string? eventType, EventSequenceNumber? sequenceNumber) => (eventType, sequenceNumber) switch
    {
        ({ } type, { } number) => $" ({type} at {number.Value})",
        ({ } type, null) => $" ({type})",
        (null, { } number) => $" (an event at {number.Value})",
        _ => string.Empty
    };

    static string NameOf(EventTypeId eventTypeId, IServiceProvider? services)
    {
        var eventTypes = services?.GetService<IEventStore>()?.EventTypes;
        return eventTypes?.HasFor(eventTypeId) == true ? eventTypes.GetClrTypeFor(eventTypeId).Name : eventTypeId.Value;
    }
}
