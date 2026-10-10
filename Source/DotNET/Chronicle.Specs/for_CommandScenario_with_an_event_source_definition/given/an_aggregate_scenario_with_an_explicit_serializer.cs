// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition.given;

public class an_aggregate_scenario_with_an_explicit_serializer : a_routed_command_scenario
{
    protected CommandScenario<RecordAggregateEntry> _aggregateScenario;
    protected IEventSerializer _serializer;

    void Establish()
    {
        _aggregateScenario = new();
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(new EntryRecorded("Existing entry"));
        _aggregateScenario.Services.AddSingleton(_serializer);
    }

    protected async Task SeedEntry()
    {
        var result = await _aggregateScenario.EventSequence.Append(
            _command.Id,
            new EntryRecorded("Existing entry"),
            eventStreamType: "entries",
            eventStreamId: _command.StreamId,
            eventSourceType: "scenario-ledger");
        result.IsSuccess.ShouldBeTrue();
    }

    async Task Destroy() => await _aggregateScenario.DisposeAsync();
}
