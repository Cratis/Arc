// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition;

public class when_executing_an_aggregate_command : given.a_routed_command_scenario
{
    CommandScenario<RecordAggregateEntry> _aggregateScenario;
    CommandResult _result;

    void Establish()
    {
        _aggregateScenario = new();
        _aggregateScenario.Services.AddSingleton(Defaults.Instance.EventSerializer);
    }

    async Task Because() => _result = await _aggregateScenario.Execute(new(_command.Id, _command.StreamId, _command.Description));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_use_the_aggregates_defined_event_source_type() => _aggregateScenario.AppendedEvents.Single().Event.Context.EventSourceType.ShouldEqual(new EventSourceType("scenario-ledger"));
    [Fact] void should_use_the_aggregates_defined_stream_type() => _aggregateScenario.AppendedEvents.Single().Event.Context.EventStreamType.ShouldEqual(new EventStreamType("entries"));
    [Fact] void should_use_the_requested_stream_id() => _aggregateScenario.AppendedEvents.Single().Event.Context.EventStreamId.ShouldEqual(new EventStreamId(_command.StreamId));

    async Task Destroy() => await _aggregateScenario.DisposeAsync();
}
