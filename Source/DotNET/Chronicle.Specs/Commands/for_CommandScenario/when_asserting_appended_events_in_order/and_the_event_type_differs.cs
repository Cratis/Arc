// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_order;

public class and_the_event_type_differs : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event()]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInOrder(new OtherAssertionEvent("first")));
    }

    [Fact] void should_not_match_only_the_common_property() => _error.ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] void should_report_the_type_difference() => _error.Message.ShouldContain("OtherAssertionEvent.Type expected");
}
