// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_from_events;

public class into_a_command_handle : Specification
{
    CommandScenario<SettleLedger> _scenario;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<SettleLedger>();
        _scenario.Given.ForEventSource(_eventSourceId).Events(new LedgerCredited(40m), new LedgerCredited(2m));
    }

    async Task Because() => await _scenario.Execute(new SettleLedger(_eventSourceId));

    [Fact] async Task should_inject_the_read_model_materialized_from_the_events() =>
        await _scenario.ShouldHaveAppendedEvent<SettleLedger, LedgerSettled>(_eventSourceId, @event => @event.Balance == 42m);
}
