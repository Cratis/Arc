// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.given;

public class a_mixed_generated_batch : Commands.given.a_generated_target
{
    CommandScenario<for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario.CreateSource> _scenario;
    protected EventsForEventSourceIdCommandResponseValueHandler _handler;
    protected EventForEventSourceId[] _events;
    protected EventSourceId _other;
    protected AppendedEvent[] _stored;
    protected Exception _error;

    void Establish()
    {
        _scenario = new CommandScenario<for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario.CreateSource>().UseDecisionReads();
        _eventLog = _scenario.EventLog;
        _other = new("existing-target");
        _handler = new(_eventLog, _eventTypes, _strategies, _eventSources);
        _events =
        [
            new(_id, new Created("first")) { EventStreamType = new("created"), Tags = ["first-tag"] },
            new(_id, new Created("second")) { EventStreamType = new("updated"), NamedTags = [new("kind", "second")] },
            new(_other, new Created("third")) { EventStreamType = new("legacy-one") },
            new(_other, new Created("fourth")) { EventStreamType = new("legacy-two") }
        ];
    }

    protected async Task SeedOccupiedSource() => await _eventLog.Append(_id, new Created("existing"), eventStreamType: new("unrelated"));

    protected async Task Execute()
    {
        _result = await _handler.Handle(_context, _events);
        _stored = (await _eventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
