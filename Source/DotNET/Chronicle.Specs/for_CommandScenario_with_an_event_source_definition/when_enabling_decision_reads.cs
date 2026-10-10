// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition;

public class when_enabling_decision_reads : given.a_routed_command_scenario
{
    CommandResult _result;

    void Establish() => _scenario.UseDecisionReads();

    async Task Because() => _result = await _scenario.Execute(_command);

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_use_the_defined_event_source_type() => _scenario.AppendedEvents.Single().Event.Context.EventSourceType.ShouldEqual(new EventSourceType("scenario-ledger"));
    [Fact] void should_resolve_the_same_route() => _scenario.EventRoutes.For<ScenarioLedger>("entries").EventStreamType.ShouldEqual(new EventStreamType("entries"));
    [Fact] void should_use_the_decision_stores_definitions()
    {
        using var provider = _scenario.Services.BuildServiceProvider();
        provider.GetRequiredService<IEventSources>().ShouldEqual(provider.GetRequiredService<IEventStore>().EventSources);
    }
}
