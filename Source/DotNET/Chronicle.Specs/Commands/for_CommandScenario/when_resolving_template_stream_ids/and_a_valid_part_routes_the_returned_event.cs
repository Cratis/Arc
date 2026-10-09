// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_resolving_template_stream_ids;

public class and_a_valid_part_routes_the_returned_event : given.a_template_report_scenario
{
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(_command);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_route_to_the_template_stream() => _scenario.AppendedEvents.Single().Event.Context.EventStreamId.ShouldEqual(new EventStreamId("reporting:2026-10-01"));
    [Fact] void should_keep_the_stream_type() => _scenario.AppendedEvents.Single().Event.Context.EventStreamType.ShouldEqual(new EventStreamType("template-reports"));
    [Fact] void should_keep_the_event_source() => _scenario.AppendedEvents.Single().Event.Context.EventSourceId.ShouldEqual(_command.Id);
}
