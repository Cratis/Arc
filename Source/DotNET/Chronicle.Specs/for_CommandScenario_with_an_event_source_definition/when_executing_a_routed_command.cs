// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition;

public class when_executing_a_routed_command : given.a_routed_command_scenario
{
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(_command);

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_use_the_defined_event_source_type() => _scenario.AppendedEvents.Single().Event.Context.EventSourceType.ShouldEqual(new EventSourceType("scenario-ledger"));
    [Fact] void should_use_the_defined_stream_type() => _scenario.AppendedEvents.Single().Event.Context.EventStreamType.ShouldEqual(new EventStreamType("entries"));
    [Fact] void should_use_the_commands_stream_id() => _scenario.AppendedEvents.Single().Event.Context.EventStreamId.ShouldEqual(new EventStreamId(_command.StreamId));
    [Fact] void should_use_the_commands_event_source_id() => _scenario.AppendedEvents.Single().Event.Context.EventSourceId.ShouldEqual(_command.Id);
}
