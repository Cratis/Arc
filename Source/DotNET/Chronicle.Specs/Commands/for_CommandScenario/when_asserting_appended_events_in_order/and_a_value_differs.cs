// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_order;

public class and_a_value_differs : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event(city: "Bergen")]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInOrder(Event(city: "Oslo")));
    }

    [Fact] void should_fail() => _error.ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] void should_report_the_nested_path_and_values() => _error.Message.ShouldContain("AssertionEvent.Address.City expected 'Oslo' but was 'Bergen'");
}
