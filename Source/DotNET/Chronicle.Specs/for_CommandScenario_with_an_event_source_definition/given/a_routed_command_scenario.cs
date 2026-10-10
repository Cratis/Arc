// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition.given;

public class a_routed_command_scenario : Specification
{
    protected CommandScenario<RecordEntry> _scenario;
    protected RecordEntry _command;

    [Cratis.Chronicle.EventSources.EventSource("scenario-ledger")]
    [EventStream("entries")]
    public class ScenarioLedger : IEventSource;

    [EventType]
    public record EntryRecorded(string Description);

    [Command]
    [EventSource<ScenarioLedger>("entries")]
    [EventStreamId("{StreamId}")]
    public record RecordEntry(EventSourceId Id, string StreamId, string Description)
    {
        public EntryRecorded Handle() => new(Description);
    }

    [EventSource<ScenarioLedger>("entries")]
    public class LedgerAggregate : AggregateRoot;

    [Command]
    public record RecordAggregateEntry(EventSourceId Id, string StreamId, string Description)
    {
        public async Task<AggregateRootCommitResult> Handle(IAggregateRootFactory factory)
        {
            var aggregate = await factory.Get<LedgerAggregate>(Id, new EventStreamId(StreamId));
            await aggregate.Apply(new EntryRecorded(Description));

            return await aggregate.Commit();
        }
    }

    void Establish()
    {
        _scenario = new();
        _command = new(EventSourceId.New(), "october", "Entry");
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
