// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_any_order;

public class and_a_duplicate_is_unexpected : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event(), Event()]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInAnyOrder(Event()));
    }

    [Fact] void should_not_collapse_actual_duplicates() => _error.ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] void should_report_the_unexpected_duplicate() => _error.Message.ShouldContain("unexpected: AssertionEvent");
}
