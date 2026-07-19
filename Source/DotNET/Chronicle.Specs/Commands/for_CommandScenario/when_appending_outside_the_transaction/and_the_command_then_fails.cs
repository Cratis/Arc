// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_outside_the_transaction;

public class and_the_command_then_fails : Specification
{
    CommandScenario<AppendOutsideTransactionThenFail> _scenario;
    CommandResult _result;
    EventSourceId _partner;

    void Establish()
    {
        _partner = EventSourceId.New();
        _scenario = new CommandScenario<AppendOutsideTransactionThenFail>();
    }

    async Task Because() => _result = await _scenario.Execute(new AppendOutsideTransactionThenFail(_partner));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_keep_the_event_appended_outside_the_transaction() => (await _scenario.EventScenario.EventLog.HasEventsFor(_partner)).ShouldBeTrue();
}
