// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Handles an ordered collection of cross-source events with exact concurrency scopes returned from a command.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
public class EventsWithConcurrencyScopesCommandResponseValueHandler(IEventLog eventLog) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is EventsWithConcurrencyScopes;

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var response = (EventsWithConcurrencyScopes)value;
        var events = response.Events
            .Select(@event => eventLog.WithCommandMetadata(@event, commandContext))
            .ToArray();

        if (CommandTransaction.TryGetActive(out var unitOfWork))
        {
            unitOfWork.AddEvents(eventLog.Id, events, response.ConcurrencyScopes);
        }
        else
        {
            var result = await eventLog.AppendMany(
                events,
                concurrencyScopes: response.ConcurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value));

            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
