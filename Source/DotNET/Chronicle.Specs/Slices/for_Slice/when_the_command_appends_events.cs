// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_the_command_appends_events : given.a_slice
{
    void Establish() => _slice.GivenEvents(new LedgerCredited(42m));

    async Task Because() => _outcome = await _slice.Run();

    [Fact] void should_allow_the_success_assertion() => _outcome.ShouldBeSuccessful();
    [Fact] async Task should_allow_ordered_event_assertions() => await _outcome.ShouldHaveAppendedEventsInOrder(new LedgerSettled(42m));
    [Fact] async Task should_allow_unordered_event_assertions() => await _outcome.ShouldHaveAppendedEventsInAnyOrder(new LedgerSettled(42m));
    [Fact] async Task should_allow_event_type_assertions() => await _outcome.ShouldHaveAppendedEvent<LedgerSettled>(_source);
    [Fact] async Task should_allow_event_predicate_assertions() => await _outcome.ShouldHaveAppendedEvent<LedgerSettled>(_source, @event => @event.Balance == 42m);
    [Fact] async Task should_allow_no_events_assertions_for_another_source() => await _outcome.ShouldNotHaveAppendedEvents(EventSourceId.New());
    [Fact] void should_make_the_completed_outcome_available_as_then() => _slice.Then.ShouldBeSame(_outcome);
}
