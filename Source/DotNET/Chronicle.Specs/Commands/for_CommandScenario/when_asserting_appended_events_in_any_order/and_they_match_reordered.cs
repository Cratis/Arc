// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_any_order;

public class and_they_match_reordered : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event("first", "Oslo", "a"), Event("second")], IncludeOther: true));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInAnyOrder(new OtherAssertionEvent("other"), Event("second"), Event("first", "Oslo", "a")));
    }

    [Fact] void should_have_executed_the_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_match_the_reordered_values() => _error.ShouldBeNull();
}
