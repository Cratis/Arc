// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_no_appended_events;

public class and_none_were_appended : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, []));
        _error = await Catch.Exception(_scenario.ShouldNotHaveAppendedEvents);
    }

    [Fact] void should_have_executed_the_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_pass() => _error.ShouldBeNull();
    [Fact] async Task should_accept_an_empty_ordered_list() => await _scenario.ShouldHaveAppendedEventsInOrder();
    [Fact] async Task should_accept_an_empty_unordered_list() => await _scenario.ShouldHaveAppendedEventsInAnyOrder();
}
