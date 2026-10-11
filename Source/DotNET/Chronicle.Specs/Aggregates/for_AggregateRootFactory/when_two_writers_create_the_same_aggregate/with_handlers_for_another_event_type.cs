// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory.when_two_writers_create_the_same_aggregate;

public class with_handlers_for_another_event_type : given.two_writers_creating_the_same_aggregate
{
    [EventType]
    record CreationGuardChanged;

    AppendResult _firstResult;
    AppendResult _secondResult;

    protected override IAggregateRootEventHandlers CreateEventHandlers()
    {
        var handlers = Substitute.For<IAggregateRootEventHandlers>();
        handlers.HasHandleMethods.Returns(true);
        handlers.EventTypes.Returns([typeof(CreationGuardChanged).GetEventType()]);
        return handlers;
    }

    protected override async Task StageEvents(TestAggregateRoot aggregateRoot)
    {
        await aggregateRoot._mutation.Apply(new Created());

        // Mutate sets HasEvents while staging the first event; the next event must keep the creation guard.
        await aggregateRoot._mutation.Apply(new Created());
    }

    async Task Because()
    {
        _firstResult = await Append(_firstWritersScope);
        _secondResult = await Append(_secondWritersScope);
    }

    [Fact] void should_let_the_first_writer_create_the_aggregate() => _firstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_guard_every_event_type_for_the_first_writer() => _firstWritersScope.EventTypes.ShouldBeNull();
    [Fact] void should_keep_the_guard_unnarrowed_after_staging_events() => _secondWritersScope.EventTypes.ShouldBeNull();
    [Fact] void should_reject_the_second_writers_duplicate_creation() => _secondResult.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_validate_the_second_writers_commit() => _secondResult.ConcurrencyCheckPerformed.ShouldBeTrue();
}
