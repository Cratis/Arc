// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_no_appended_events;

public class and_a_previous_execution_appended : given.an_appended_event_assertion
{
    async Task Establish() => await _scenario.Execute(new(_source, [Event("previous")]));

    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, []));
        _error = await Catch.Exception(_scenario.ShouldNotHaveAppendedEvents);
    }

    [Fact] void should_only_consider_the_latest_execution() => _error.ShouldBeNull();
}
