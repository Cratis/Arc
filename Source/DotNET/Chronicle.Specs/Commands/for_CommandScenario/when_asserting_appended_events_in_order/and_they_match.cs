// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_order;

public class and_they_match : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, [Event("first", "Oslo", "a", "b"), Event("second")]));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInOrder(Event("first", "Oslo", "a", "b"), Event("second")));
    }

    [Fact] void should_have_executed_the_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_compare_nested_objects_and_array_elements_by_value() => _error.ShouldBeNull();
}
