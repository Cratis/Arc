// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition;

public class when_resolving_a_route_in_default_mode : given.a_routed_command_scenario
{
    EventRoute _route;

    void Because() => _route = _scenario.EventRoutes.For<ScenarioLedger>("entries");

    [Fact] void should_resolve_the_defined_event_source_type() => _route.EventSourceType.ShouldEqual(new EventSourceType("scenario-ledger"));
    [Fact] void should_resolve_the_defined_stream_type() => _route.EventStreamType.ShouldEqual(new EventStreamType("entries"));
    [Fact] void should_retain_the_definition_type() => _route.EventSource.ShouldEqual(typeof(ScenarioLedger));
}
