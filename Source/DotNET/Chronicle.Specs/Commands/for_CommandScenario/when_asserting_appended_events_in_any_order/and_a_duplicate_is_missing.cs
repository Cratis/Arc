// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_any_order;

public class and_a_duplicate_is_missing : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event()]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInAnyOrder(Event(), Event()));
    }

    [Fact] void should_not_reuse_a_match_for_two_expected_events() => _error.ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] void should_report_the_missing_duplicate() => _error.Message.ShouldContain("missing: AssertionEvent");
}
