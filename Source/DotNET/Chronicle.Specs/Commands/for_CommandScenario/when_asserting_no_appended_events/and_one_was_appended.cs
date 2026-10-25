// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_no_appended_events;

public class and_one_was_appended : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event()]));
        _error = await Catch.Exception(_scenario.ShouldNotHaveAppendedEvents);
    }

    [Fact] void should_fail() => _error.ShouldBeOfExactType<EventSequenceAssertionException>();
    [Fact] void should_report_the_count() => _error.Message.ShouldContain("Expected no events to be appended, but 1 were:");
    [Fact] void should_identify_the_event_and_source() => _error.Message.ShouldContain($"{nameof(AssertionEvent)}@{_source}");
    [Fact] async Task should_also_fail_for_the_source() => (await Catch.Exception(() => _scenario.ShouldNotHaveAppendedEvents(_source))).ShouldBeOfExactType<EventSequenceAssertionException>();
}
