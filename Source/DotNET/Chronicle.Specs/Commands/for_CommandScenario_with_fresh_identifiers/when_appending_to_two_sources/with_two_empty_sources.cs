// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_appending_to_two_sources;

public class with_two_empty_sources : given.two_generated_sources
{
    bool _firstHasEvents;
    bool _secondHasEvents;

    async Task Because()
    {
        _result = await _scenario.Execute(_command);
        _firstHasEvents = await _scenario.EventLog.HasEventsFor(_first);
        _secondHasEvents = await _scenario.EventLog.HasEventsFor(_second);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_create_the_first_source() => _firstHasEvents.ShouldBeTrue();
    [Fact] void should_create_the_second_source() => _secondHasEvents.ShouldBeTrue();
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
