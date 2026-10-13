// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_seeded_events : given.an_observed_account
{
    void Establish() => _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(42m));

    async Task Because() => _observation = await _scenario.Observe(nameof(ObservedAccount.All));

    [Fact] void should_emit_the_projected_events() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m));
    [Fact] void should_succeed() => _observation.Initial.IsSuccess.ShouldBeTrue();
}
