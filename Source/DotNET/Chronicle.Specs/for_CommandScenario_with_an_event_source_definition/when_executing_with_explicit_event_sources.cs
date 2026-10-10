// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition;

public class when_executing_with_explicit_event_sources : given.a_routed_command_scenario
{
    IEventSources _eventSources;
    CommandResult _result;

    void Establish()
    {
        _eventSources = Substitute.For<IEventSources>();
        _eventSources.GetFor(typeof(ScenarioLedger)).Returns(new EventSourceDefinition(typeof(ScenarioLedger), "explicit-ledger", "", ConcurrencyDimensions.None, [new("entries", "", ConcurrencyDimensions.None)]));
        _scenario.Services.AddSingleton(_eventSources);
    }

    async Task Because() => _result = await _scenario.Execute(_command);

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_use_the_explicit_definitions_for_the_command() => _eventSources.Received(1).GetFor(typeof(ScenarioLedger));
    [Fact] void should_use_the_explicit_definition_for_routes() => _scenario.EventRoutes.For<ScenarioLedger>("entries").EventSourceType.ShouldEqual(new EventSourceType("explicit-ledger"));
}
