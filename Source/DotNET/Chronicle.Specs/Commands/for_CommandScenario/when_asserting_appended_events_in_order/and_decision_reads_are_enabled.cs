// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_order;

public class and_decision_reads_are_enabled : given.an_appended_event_assertion
{
    void Establish()
    {
        _scenario.Given.ForEventSource(_source).Events(Event("seed"));
    }

    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event("command")]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInOrder(Event("command")));
    }

    [Fact] void should_have_executed_the_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_follow_the_replaced_append_capture() => _error.ShouldBeNull();
}
