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
        // Capture the scoped tail before reading events, so an append during the read cannot be accepted as seen.
        var tailSequenceNumber = await aggregateRootContext.EventSequence.GetTailSequenceNumber(
            aggregateRootContext.EventSourceId,
            aggregateRootContext.EventSourceType,
            aggregateRootContext.EventStreamType,
            aggregateRootContext.EventStreamId);

        // Read with the same source type, stream type and stream id the tail above and the commit's concurrency scope
        // use, so Chronicle narrows all three with one predicate and no event outside the guarded scope changes state
        // (#2796). Chronicle treats the default source type, the default stream id and the 'All' stream type as
        // "any" in both places. This read starts at the first event, so skip what this context has already handled.
        var events = (await aggregateRootContext.EventSequence.GetForEventSourceIdAndEventTypes(
                aggregateRootContext.EventSourceId,
                eventHandlers.EventTypes,
                aggregateRootContext.EventStreamType,
                aggregateRootContext.EventStreamId,
                aggregateRootContext.EventSourceType))
            .Where(_ => _.Context.SequenceNumber >= aggregateRootContext.NextSequenceNumber)
            .ToArray();

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
