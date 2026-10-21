// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_decision_reads : given.a_shared_command_scenario
{
    readonly EventSourceId _pinnedId = EventSourceId.New();
    QueryResult _next = default!;

    async Task Establish()
    {
        _commands.UseDecisionReads();
        ShareServices();
        _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m));
        _commands.Given.ForEventSource(_pinnedId).ReadModel(new ObservedAccount(_pinnedId.Value, 20m));
        _observation = await _scenario.Observe(nameof(ObservedAccount.All));
    }

    async Task Because()
    {
        (await _commands.Execute(new ChangeObservedAccount(_id, 42m))).IsSuccess.ShouldBeTrue();
        _next = await _observation.Next();
    }

    [Fact] void should_emit_seeded_events_and_pins() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 10m), new ObservedAccount(_pinnedId.Value, 20m));
    [Fact] void should_emit_command_updates_and_keep_pins() => ((IEnumerable<ObservedAccount>)_next.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m), new ObservedAccount(_pinnedId.Value, 20m));
    [Fact] void should_capture_only_the_command_event() => _commands.AppendedEvents.Count.ShouldEqual(1);
}
