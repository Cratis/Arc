// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Handles an ordered collection of cross-source events with exact concurrency scopes returned from a command.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
public class EventsWithConcurrencyScopesCommandResponseValueHandler(IEventLog eventLog) : ICommandResponseValueHandler, ICommandResponseValueHandler<EventsWithConcurrencyScopes>
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is EventsWithConcurrencyScopes;

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var response = (EventsWithConcurrencyScopes)value;
        if (response.Events.Count == 0 && response.ConcurrencyScopes.Count == 0)
        {
            return CommandResult.Success(commandContext.CorrelationId);
        }

        var events = response.Events
            .Select(@event => eventLog.WithCommandMetadata(@event, commandContext))
            .ToArray();

        if (CommandTransaction.TryGetActive(out var unitOfWork))
        {
            unitOfWork.AddEvents(eventLog.Id, events, response.ConcurrencyScopes);
        }
        else
        {
            // CHR0001 misfires here: it assumes the second syntactic argument to AppendMany is always the
            // event collection, but this call's second argument is the named concurrencyScopes dictionary,
            // which binds to a later parameter. The analyzer then flags EventSourceId, one of that
            // dictionary's type arguments, as if it needed an [EventType] attribute.
#pragma warning disable CHR0001
            var result = await eventLog.AppendMany(
                events,
                concurrencyScopes: response.ConcurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value));
#pragma warning restore CHR0001

            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
