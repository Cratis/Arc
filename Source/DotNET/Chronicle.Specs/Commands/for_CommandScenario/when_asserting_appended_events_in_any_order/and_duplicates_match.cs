// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_any_order;

public class and_duplicates_match : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event(), Event("second"), Event()]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInAnyOrder(Event(), Event(), Event("second")));
    }

    [Fact] void should_match_each_duplicate_once() => _error.ShouldBeNull();
}
