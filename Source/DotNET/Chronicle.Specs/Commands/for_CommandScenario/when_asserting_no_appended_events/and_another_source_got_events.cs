// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_no_appended_events;

public class and_another_source_got_events : given.an_appended_event_assertion
{
    EventSourceId _otherSource;

    async Task Establish()
    {
        _otherSource = EventSourceId.New();
        await _scenario.EventScenario.Given.ForEventSource(_source).Events(Event("seed"));
    }

    async Task Because()
    {
        _result = await _scenario.Execute(new(_otherSource, [Event("command")]));
        _error = await Catch.Exception(() => _scenario.ShouldNotHaveAppendedEvents(_source));
    }

    [Fact] void should_exclude_the_seeded_event_at_the_checked_source() => _error.ShouldBeNull();
    [Fact] async Task should_still_find_the_command_event_at_the_other_source() => (await Catch.Exception(() => _scenario.ShouldNotHaveAppendedEvents(_otherSource))).ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] async Task should_exclude_givens_from_exact_assertions() => await _scenario.ShouldHaveAppendedEventsInOrder(Event("command"));
    [Fact] async Task should_exclude_givens_from_unordered_assertions() => await _scenario.ShouldHaveAppendedEventsInAnyOrder(Event("command"));
}
