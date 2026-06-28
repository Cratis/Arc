// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_from_events;

public class for_a_read_model_without_a_parameterless_constructor : Specification
{
    CommandScenario<SettleStatement> _scenario;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<SettleStatement>();
        _scenario.Given.ForEventSource(_eventSourceId).Events(new LedgerCredited(7m));
    }

    async Task Because() => await _scenario.Execute(new SettleStatement(_eventSourceId));

    [Fact] async Task should_materialize_the_read_model_from_the_events() =>
        await _scenario.ShouldHaveAppendedEvent<SettleStatement, LedgerSettled>(_eventSourceId, @event => @event.Balance == 7m);
}
