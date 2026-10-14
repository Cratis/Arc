// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_a_cross_source_join : Specification
{
    readonly QueryScenario<ObservedOrder> _scenario = new();
    readonly EventSourceId _orderId = EventSourceId.New();
    readonly EventSourceId _accountId = EventSourceId.New();
    QueryObservation _observation = default!;
    QueryResult _next = default!;

    async Task Establish()
    {
        _scenario.Given.ForEventSource(_accountId).Events(new ObservedAccountChanged(10m));
        _scenario.Given.ForEventSource(_orderId).Events(new ObservedOrderPlaced(_accountId.Value, 5m));
        _observation = await _scenario.Observe(nameof(ObservedOrder.All));
    }

    async Task Because()
    {
        _scenario.Given.ForEventSource(_accountId).Events(new ObservedAccountChanged(42m));
        _next = await _observation.Next();
    }

    [Fact] void should_join_the_seeded_account_balance() => ((IEnumerable<ObservedOrder>)_observation.Initial.Data).ShouldContainOnly(new ObservedOrder(_orderId.Value, _accountId.Value, 5m, 10m));
    [Fact] void should_emit_the_updated_joined_balance() => ((IEnumerable<ObservedOrder>)_next.Data).ShouldContainOnly(new ObservedOrder(_orderId.Value, _accountId.Value, 5m, 42m));

    void Destroy() => _scenario.Dispose();
}
