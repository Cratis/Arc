// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_from_events;

public class feeding_multiple_read_models : Specification
{
    CommandScenario<SummarizeLedger> _scenario;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<SummarizeLedger>();
        _scenario.Given.ForEventSource(_eventSourceId).Events(new LedgerCredited(10m));
    }

    async Task Because() => await _scenario.Execute(new SummarizeLedger(_eventSourceId));

    [Fact] async Task should_materialize_every_read_model_built_from_the_events_without_naming_any() =>
        await _scenario.ShouldHaveAppendedEvent<SummarizeLedger, LedgerSummarized>(
            _eventSourceId,
            @event => @event.BalanceTotal == 10m && @event.StatementTotal == 10m);
}
