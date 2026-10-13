// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_a_filter : given.an_observed_account
{
    void Establish()
    {
        _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(42m));
        _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ObservedAccountChanged(-10m));
        _scenario.Given.ForEventSource(EventSourceId.New()).ReadModel(new ObservedAccount("pinned", -20m));
    }

    async Task Because() => _observation = await _scenario.Observe(nameof(ObservedAccount.Positive));

    [Fact] void should_filter_events_and_pins() => ((IEnumerable<ObservedAccount>)_observation.Initial.Data).ShouldContainOnly(new ObservedAccount(_id.Value, 42m));
}
