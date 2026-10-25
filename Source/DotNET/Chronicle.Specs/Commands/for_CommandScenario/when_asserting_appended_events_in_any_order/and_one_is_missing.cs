// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_any_order;

public class and_one_is_missing : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event("first"), Event("unexpected")]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInAnyOrder(Event("first"), Event("missing")));
    }

    [Fact] void should_fail() => _error.ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] void should_report_missing_values() => _error.Message.ShouldContain("missing: AssertionEvent {\"Name\":\"missing\"");
    [Fact] void should_report_unexpected_values() => _error.Message.ShouldContain("unexpected: AssertionEvent {\"Name\":\"unexpected\"");
}
