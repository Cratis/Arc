// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_fluent_set_and_add : Specification
{
    readonly QueryScenario<ObservedCounter> _scenario = new();
    readonly EventSourceId _id = EventSourceId.New();
    ObservedCounter _observed = default!;
    ObservedCounter _resolved = default!;

    void Establish() => _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m), new ObservedAmountAdded(5m), new ObservedAmountAdded(2m));

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservedCounter.All));
        _observed = ((IEnumerable<ObservedCounter>)observation.Initial.Data).Single();
        await using var services = _scenario.Services.BuildServiceProvider();
        _resolved = await services.GetRequiredService<IReadModels>().GetInstanceById<ObservedCounter>(new ReadModelKey(_id.Value));
    }

    [Fact] void should_apply_set_and_add() => _observed.ShouldEqual(new ObservedCounter(_id.Value, 17m));
    [Fact] void should_match_instance_reads() => _observed.ShouldEqual(_resolved);

    void Destroy() => _scenario.Dispose();
}
