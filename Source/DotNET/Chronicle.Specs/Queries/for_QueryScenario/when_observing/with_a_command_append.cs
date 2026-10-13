// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_a_command_append : given.a_shared_command_scenario
{
    QueryResult _next = default!;

    async Task Establish()
    {
        _commands.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m));
        ShareServices();
        _observation = await _scenario.Observe(nameof(ObservedAccount.All));
    }

    async Task Because()
    {
        (await _commands.Execute(new ChangeObservedAccount(_id, 42m))).IsSuccess.ShouldBeTrue();
        _next = await _observation.Next();
    }

    [Fact] void should_emit_the_seeded_initial_collection() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 10m));
    [Fact] void should_emit_the_updated_collection() => ((IEnumerable<ObservedAccount>)_next.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m));
    [Fact] void should_capture_only_the_command_event() => _commands.AppendedEvents.Count.ShouldEqual(1);
    [Fact] async Task should_keep_the_command_tail_unchanged() => (await _commands.EventLog.GetTailSequenceNumber()).ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_keep_observation_seeds_out_of_the_command_log() => (await _commands.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Select(@event => @event.Content).ShouldContainOnly(new ObservedAccountChanged(42m));
}
