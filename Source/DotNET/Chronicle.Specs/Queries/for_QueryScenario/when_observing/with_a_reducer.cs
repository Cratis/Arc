// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_a_reducer : Specification
{
    readonly QueryScenario<ObservedReducedAccount> _scenario = new();
    readonly EventSourceId _id = EventSourceId.New();
    readonly EventSourceId _pinnedId = EventSourceId.New();
    QueryObservation _observation = default!;
    QueryResult _next = default!;

    async Task Establish()
    {
        _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m), new ObservedAccountChanged(5m));
        _scenario.Given.ForEventSource(_pinnedId).Events(new ObservedAccountChanged(1m));
        _scenario.Given.ForEventSource(_pinnedId).ReadModel(new ObservedReducedAccount(_pinnedId.Value, 20m));
        _observation = await _scenario.Observe(nameof(ObservedReducedAccount.All));
    }

    async Task Because()
    {
        _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(27m));
        _next = await _observation.Next();
    }

    [Fact] void should_reduce_each_source_and_keep_the_pin() => ((IEnumerable<ObservedReducedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedReducedAccount(_id.Value, 15m), new ObservedReducedAccount(_pinnedId.Value, 20m));
    [Fact] void should_emit_the_accumulated_balance_after_seeding() => ((IEnumerable<ObservedReducedAccount>)_next.Data).ShouldContainOnly(new ObservedReducedAccount(_id.Value, 42m), new ObservedReducedAccount(_pinnedId.Value, 20m));

    void Destroy() => _scenario.Dispose();
}
