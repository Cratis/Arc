// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_later_pinned_state : given.an_observed_account
{
    QueryResult _next = default!;

    async Task Establish()
    {
        _scenario.Given.ForEventSource(_id).ReadModel(new ObservedAccount(_id.Value, 10m));
        _observation = await _scenario.Observe(nameof(ObservedAccount.All));
    }

    async Task Because()
    {
        _scenario.Given.ForEventSource(_id).ReadModel(new ObservedAccount(_id.Value, 42m));
        _next = await _observation.Next();
    }

    [Fact] void should_emit_the_initial_pin_without_events() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 10m));
    [Fact] void should_emit_the_replacement_pin() => ((IEnumerable<ObservedAccount>)_next.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m));
}
