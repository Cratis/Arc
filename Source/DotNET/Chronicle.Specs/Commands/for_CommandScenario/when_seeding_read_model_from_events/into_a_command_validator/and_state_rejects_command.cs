// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_from_events.into_a_command_validator;

public class and_state_rejects_command : Specification
{
    CommandScenario<SettleLedger> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<SettleLedger>();
        _scenario.Given.ForEventSource(_eventSourceId).Events(new LedgerCredited(40m), new LedgerDebited(40m));
    }

    async Task Because() => _result = await _scenario.Execute(new SettleLedger(_eventSourceId));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_validation_error_from_the_materialized_read_model() => _result.ValidationResults.First().Message.ShouldEqual("Ledger has no funds to settle.");
    [Fact] void should_not_have_appended_events() => _scenario.AppendedEvents.ShouldBeEmpty();
}
