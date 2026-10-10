// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class without_handle_methods : given.an_aggregate_mutation
{
    [EventType]
    record Changed;

    ConcurrencyScope _scope;

    void Establish()
    {
        var handlers = Substitute.For<IAggregateRootEventHandlers>();
        handlers.EventTypes.Returns(ImmutableList<EventType>.Empty);
        var mutator = new AggregateRootMutator(
            _aggregateRootContext,
            Substitute.For<IEventStore>(),
            Substitute.For<IEventSerializer>(),
            handlers,
            Substitute.For<ICorrelationIdAccessor>());
        _mutation = new AggregateRootMutation(_aggregateRootContext, mutator, _eventSequence);
        _unitOfWork.When(_ => _.AddEvent(
                _eventSequenceId,
                _eventSourceId,
                Arg.Any<Changed>(),
                Arg.Any<Causation>(),
                _eventStreamType,
                _eventStreamId,
                _eventSourceType,
                Arg.Any<ConcurrencyScope>()))
            .Do(call => _scope = call.Arg<ConcurrencyScope>());
    }

    Task Because() => _mutation.Apply(new Changed());

    [Fact] void should_not_narrow_the_guard_to_an_empty_event_type_set() => _scope.EventTypes.ShouldBeNull();
}
