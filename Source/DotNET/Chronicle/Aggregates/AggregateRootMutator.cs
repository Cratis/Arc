// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutator"/> for <see cref="IAggregateRoot"/>.
/// </summary>
/// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> to work with.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> to work with.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="eventHandlers">The <see cref="IAggregateRootEventHandlers"/> for the aggregate root.</param>
/// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> for correlation id.</param>
public class AggregateRootMutator(
    IAggregateRootContext aggregateRootContext,
    IEventStore eventStore,
    IEventSerializer eventSerializer,
    IAggregateRootEventHandlers eventHandlers,
    ICorrelationIdAccessor correlationIdAccessor) : IAggregateRootMutator
{
    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        // Capture the tail of the scope the commit guards before reading events, so an append during the read cannot
        // be accepted as seen.
        var scope = AggregateRootGuardedScope.For(aggregateRootContext);
        var tailSequenceNumber = await aggregateRootContext.EventSequence.GetTailSequenceNumber(
            aggregateRootContext.EventSourceId,
            scope.EventSourceType,
            scope.EventStreamType,
            scope.EventStreamId);

        // Whether the aggregate already exists is still decided by its own stream, not by every stream the commit
        // guards, so an event for the same id elsewhere does not make an undeclared aggregate stop being new.
        var ownTailSequenceNumber = scope.IsOwnStreamOf(aggregateRootContext)
            ? tailSequenceNumber
            : await aggregateRootContext.EventSequence.GetTailSequenceNumber(
                aggregateRootContext.EventSourceId,
                aggregateRootContext.EventSourceType,
                aggregateRootContext.EventStreamType,
                aggregateRootContext.EventStreamId);

        IEnumerable<AppendedEvent> events;
        if (aggregateRootContext.HasDeclaredEventSource())
        {
            // A declared aggregate keeps only the events of its own source type, stream type and stream id (#2796).
            // The read starts at the first event, so skip what this context has already handled.
            events = (await aggregateRootContext.EventSequence.GetForEventSourceIdAndEventTypes(
                    aggregateRootContext.EventSourceId,
                    eventHandlers.EventTypes,
                    aggregateRootContext.EventStreamType,
                    aggregateRootContext.EventStreamId,
                    aggregateRootContext.EventSourceType))
                .Where(_ =>
                    _.Context.SequenceNumber >= aggregateRootContext.NextSequenceNumber &&
                    _.Context.EventSourceType == aggregateRootContext.EventSourceType &&
                    _.Context.EventStreamType == aggregateRootContext.EventStreamType &&
                    _.Context.EventStreamId == aggregateRootContext.EventStreamId)
                .ToArray();
        }
        else
        {
            // Every handled event for the event source id, whatever stream it is in - the guarded scope covers it all.
            events = await aggregateRootContext.EventSequence.GetFromSequenceNumber(aggregateRootContext.NextSequenceNumber, aggregateRootContext.EventSourceId, eventHandlers.EventTypes);
        }

        if (eventHandlers.HasHandleMethods)
        {
            var deserializedEventsTasks = events.Select(async _ =>
            {
                var @event = await eventSerializer.Deserialize(_);
                return new EventAndContext(@event, _.Context);
            }).ToArray();

            var deserializedEvents = await Task.WhenAll(deserializedEventsTasks);

            // Scenario is when state is partially modified before throwing an exception.
            await eventHandlers.Handle(aggregateRootContext.AggregateRoot, deserializedEvents, handledEventAndContext =>
            {
                var nextSequenceNumber = handledEventAndContext.Context.SequenceNumber.Next();
                if (handledEventAndContext.Context.SequenceNumber == EventSequenceNumber.Unavailable)
                {
                    return;
                }
                if (nextSequenceNumber.IsActualValue && nextSequenceNumber > aggregateRootContext.NextSequenceNumber)
                {
                    aggregateRootContext.HasEvents = true;
                    aggregateRootContext.NextSequenceNumber = nextSequenceNumber;
                }
            });
        }

        if (tailSequenceNumber.IsActualValue)
        {
            aggregateRootContext.TailEventSequenceNumber = tailSequenceNumber;
        }

        if (ownTailSequenceNumber.IsActualValue)
        {
            aggregateRootContext.HasEvents = true;
        }
    }

    /// <inheritdoc/>
    public async Task Mutate(object @event)
    {
        if (eventHandlers.HasHandleMethods)
        {
            aggregateRootContext.HasEvents = true;
            await eventHandlers.Handle(
                aggregateRootContext.AggregateRoot,
                [
                    new EventAndContext(
                        @event,
                        EventContext.From(
                            eventStore.Name,
                            eventStore.Namespace,
                            eventStore.EventTypes.GetEventTypeFor(@event.GetType()),
                            aggregateRootContext.EventSourceType,
                            aggregateRootContext.EventSourceId,
                            aggregateRootContext.EventStreamType,
                            aggregateRootContext.EventStreamId,
                            EventSequenceNumber.Unavailable,
                            correlationIdAccessor.Current))
                ]);
        }
    }

    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;
}
