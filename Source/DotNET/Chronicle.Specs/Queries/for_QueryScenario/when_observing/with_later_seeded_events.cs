// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_later_seeded_events : given.an_observed_account
{
    QueryResult _next = default!;

    async Task Establish() => _observation = await _scenario.Observe(nameof(ObservedAccount.All));

    async Task Because()
    {
        _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(42m));
        _next = await _observation.Next();
    }

    [Fact] void should_emit_an_empty_initial_collection() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldBeEmpty();
    [Fact] void should_emit_the_seeded_event() => ((IEnumerable<ObservedAccount>)_next.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m));
}
