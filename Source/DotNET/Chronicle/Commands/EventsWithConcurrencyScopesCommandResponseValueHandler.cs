// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Handles an ordered collection of cross-source events with exact concurrency scopes returned from a command.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventSources">The registered event source definitions.</param>
[method: ActivatorUtilitiesConstructor]
public class EventsWithConcurrencyScopesCommandResponseValueHandler(IEventLog eventLog, IEventSources eventSources) : ICommandResponseValueHandler, ICommandResponseValueHandler<EventsWithConcurrencyScopes>
{
    /// <summary>
    /// Initializes a handler without validation of per-event routing against event source definitions.
    /// </summary>
    /// <param name="eventLog">The event log.</param>
    /// <remarks>Use the constructor taking <see cref="IEventSources"/> to validate legacy wrapper routes.</remarks>
    [Obsolete("This constructor does not validate per-event routing against event source definitions; use the constructor that takes IEventSources.")]
    public EventsWithConcurrencyScopesCommandResponseValueHandler(IEventLog eventLog)
        : this(eventLog, null!)
    {
    }

    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is EventsWithConcurrencyScopes;

    /// <inheritdoc/>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">A legacy wrapper route names a known definition and an undeclared stream on a definition-routed command.</exception>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var response = (EventsWithConcurrencyScopes)value;
        if (response.Events.Count == 0 && response.ConcurrencyScopes.Count == 0)
        {
            return CommandResult.Success(commandContext.CorrelationId);
        }

        var events = response.Events
            .Select(@event => eventLog.WithCommandMetadata(@event, commandContext, eventSources))
            .ToArray();

        var freshness = FreshIdentifierScopes.For(commandContext);
        var scopes = freshness.ResolveBatch(eventLog.Id, events, response.ConcurrencyScopes);
        if (ReadProtectionMeasurement.Current is { } measurement)
        {
            measurement.Try(() =>
            {
                foreach (var scope in scopes) measurement.Scope(scope.Key, scope.Value, "Explicit scope");
            });
        }
        if (CommandTransaction.TryGetActive(out var unitOfWork))
        {
            unitOfWork.AddEvents(eventLog.Id, events, scopes);
            foreach (var target in events.Select(@event => @event.EventSourceId).Distinct())
            {
                freshness.Enrolled(unitOfWork, eventLog.Id, target);
            }
        }
        else
        {
            CommandTransaction.RefuseImmediateAppend();
            var concurrencyScopes = scopes.ToDictionary(_ => _.Key, _ => _.Value);
            var result = events.Any(_ => _.NamedTags.Any())
                ? await eventLog.AppendManyWithNamedTags(events, [], concurrencyScopes: concurrencyScopes)
                : await eventLog.AppendMany(events, concurrencyScopes: concurrencyScopes);

            if (!result.IsSuccess)
            {
                return result.ToCommandResult(commandContext, eventLog.Id);
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
