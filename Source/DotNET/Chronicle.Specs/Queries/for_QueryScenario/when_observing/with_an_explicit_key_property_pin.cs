// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_an_explicit_key_property_pin : Specification
{
    readonly QueryScenario<ObservedKeyedAccount> _scenario = new();
    readonly EventSourceId _id = EventSourceId.New();
    readonly ObservedKeyedAccount _pin;
    QueryObservation _observation = default!;

    public with_an_explicit_key_property_pin() => _pin = new(_id.Value, 42m);

    void Establish()
    {
        _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m));
        _scenario.Given.ForEventSource(_id).ReadModel(_pin);
    }

    async Task Because()
    {
        _observation = await _scenario.Observe(nameof(ObservedKeyedAccount.All));
        Assert.True(_observation.Initial.IsSuccess, string.Join("; ", _observation.Initial.ExceptionMessages));
    }

    [Fact] void should_use_the_explicit_key_instead_of_requiring_Id() => ((IEnumerable<ObservedKeyedAccount>)_observation.Initial.Data).ShouldContainOnly(_pin);

    void Destroy() => _scenario.Dispose();
}
