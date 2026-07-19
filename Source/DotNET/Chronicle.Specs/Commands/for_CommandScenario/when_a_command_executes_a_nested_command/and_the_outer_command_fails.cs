// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_a_command_executes_a_nested_command;

public class and_the_outer_command_fails : Specification
{
    CommandScenario<ExecuteNestedThenFail> _scenario;
    CommandResult _result;
    EventSourceId _outer;
    EventSourceId _nested;

    void Establish()
    {
        _outer = EventSourceId.New();
        _nested = EventSourceId.New();
        _scenario = new CommandScenario<ExecuteNestedThenFail>();
    }

    async Task Because() => _result = await _scenario.Execute(new ExecuteNestedThenFail(_outer, _nested));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_roll_back_the_outer_append() => (await _scenario.EventScenario.EventLog.HasEventsFor(_outer)).ShouldBeFalse();
    [Fact] async Task should_roll_back_the_nested_append() => (await _scenario.EventScenario.EventLog.HasEventsFor(_nested)).ShouldBeFalse();
}
