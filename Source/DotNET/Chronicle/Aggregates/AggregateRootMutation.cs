// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutation"/>.
/// </summary>
/// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> for the aggregate root.</param>
/// <param name="mutator">The <see cref="IAggregateRootMutator"/> for the aggregate root.</param>
/// <param name="eventSequence">The <see cref="IEventSequence"/> for the aggregate root.</param>
public class AggregateRootMutation(
    IAggregateRootContext aggregateRootContext,
    IAggregateRootMutator mutator,
    IEventSequence eventSequence) : IAggregateRootMutation
{
    /// <summary>
    /// The causation aggregate root type property.
    /// </summary>
    public const string AggregateRootCausationTypeProperty = "aggregateRootType";

    /// <summary>
    /// The event sequence id causation property.
    /// </summary>
    public const string CausationEventSequenceIdProperty = "eventSequenceId";

    /// <summary>
    /// The causation type for the aggregate root.
    /// </summary>
    public static readonly CausationType CausationType = "AggregateRoot";

    /// <inheritdoc/>
    public EventSourceId EventSourceId => aggregateRootContext.EventSourceId;

    /// <inheritdoc/>
    public IImmutableList<object> UncommittedEvents { get; private set; } = ImmutableList<object>.Empty;

    /// <inheritdoc/>
    public bool HasEvents => UncommittedEvents.Count > 0;

    /// <inheritdoc/>
    public IAggregateRootMutator Mutator => mutator;

    /// <inheritdoc/>
    public async Task Apply(object @event)
    {
        @event.GetType().ValidateEventType();
        var causation = new Causation(DateTimeOffset.Now, CausationType, new Dictionary<string, string>
        {
            { AggregateRootCausationTypeProperty, aggregateRootContext.AggregateRoot.GetType().AssemblyQualifiedName! },
            { CausationEventSequenceIdProperty, eventSequence.Id }
        });

        var concurrencyScope = new ConcurrencyScope(
            aggregateRootContext.TailEventSequenceNumber,
            EventSourceId,
            aggregateRootContext.EventStreamType,
            aggregateRootContext.EventStreamId,
            aggregateRootContext.EventSourceType);

        aggregateRootContext.UnitOfWOrk.AddEvent(
            eventSequence.Id,
            EventSourceId,
            @event,
            causation,
            aggregateRootContext.EventStreamType,
            aggregateRootContext.EventStreamId,
            aggregateRootContext.EventSourceType,
            concurrencyScope);
        UncommittedEvents = UncommittedEvents.Add(@event);

        await mutator.Mutate(@event);
    }

    /// <inheritdoc/>
    public async Task<AggregateRootCommitResult> Commit()
    {
        var events = UncommittedEvents;
        var eventCount = events.Count;
        await aggregateRootContext.UnitOfWOrk.Commit();

        IEnumerable<EventSequenceNumber> sequenceNumbers = [];
        if (aggregateRootContext.UnitOfWOrk.TryGetLastCommittedEventSequenceNumber(
                out var lastCommittedEventSequenceNumber))
        {
            aggregateRootContext.NextSequenceNumber = lastCommittedEventSequenceNumber + 1;

            // Only derive sequence numbers when this aggregate's events are the whole unit of work. When the unit of
            // work also carries events from elsewhere — for example a second aggregate committed through the same
            // shared unit of work — the tail-minus-count arithmetic would attribute the wrong numbers, so report none
            // rather than fabricate them.
            if (eventCount > 0 && aggregateRootContext.UnitOfWOrk.GetEvents().Count() == eventCount)
            {
                var firstSequenceNumber = lastCommittedEventSequenceNumber.Value - (ulong)(eventCount - 1);
                sequenceNumbers = Enumerable.Range(0, eventCount)
                    .Select(i => (EventSequenceNumber)(firstSequenceNumber + (ulong)i))
                    .ToArray();
            }
        }

        UncommittedEvents = ImmutableList<object>.Empty;

        // Report this aggregate's own events, not the whole unit of work, so Events and SequenceNumbers correspond
        // one-to-one. Violations and errors are legitimately unit-of-work wide and stay sourced from it.
        return AggregateRootCommitResult.CreateFrom(aggregateRootContext.UnitOfWOrk, events, sequenceNumbers);
    }
}
