// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_an_immediate_append_is_followed_by_a_failure;

public class and_the_command_is_executed : Specification
{
    CommandScenario<AppendImmediatelyThenFail> _scenario;
    CommandResult _result;
    EventSourceId _partner;

    void Establish()
    {
        _partner = EventSourceId.New();
        _scenario = new CommandScenario<AppendImmediatelyThenFail>();
    }

    async Task Because() => _result = await _scenario.Execute(new AppendImmediatelyThenFail(_partner));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_keep_the_immediately_appended_event() => (await _scenario.EventScenario.EventLog.HasEventsFor(_partner)).ShouldBeTrue();
}
