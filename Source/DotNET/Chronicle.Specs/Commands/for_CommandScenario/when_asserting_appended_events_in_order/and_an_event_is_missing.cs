// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_order;

public class and_an_event_is_missing : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event()]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInOrder(Event(), new OtherAssertionEvent("other")));
    }

    [Fact] void should_fail() => _error.ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] void should_report_the_missing_position() => _error.Message.ShouldContain("First difference at position 1");
    [Fact] void should_explain_that_the_actual_event_is_missing() => _error.Message.ShouldContain("but was '<missing>'");
}
