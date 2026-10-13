// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_model_bound_children : Specification
{
    readonly QueryScenario<ObservedShelf> _scenario = new();
    readonly EventSourceId _id = EventSourceId.New();
    QueryObservation _observation = default!;
    ObservedShelf _resolved = default!;

    void Establish() => _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(42m), new ObservedBookAdded("first", "A book"), new ObservedBookAdded("second", "Another book"));

    async Task Because()
    {
        await using var services = _scenario.Services.BuildServiceProvider();
        _resolved = await services.GetRequiredService<IReadModels>().GetInstanceById<ObservedShelf>(new ReadModelKey(_id.Value));

        // Legacy observation cannot share EventScenario's owning storage until https://github.com/Cratis/Chronicle/issues/4816.
        _observation = await _scenario.Observe(nameof(ObservedShelf.All));
    }

    [Fact] void should_project_the_parent_in_instance_reads() => _resolved.Balance.ShouldEqual(42m);
    [Fact] void should_project_children_in_instance_reads() => _resolved.Books.ShouldContainOnly(new ObservedBook("first", "A book"), new ObservedBook("second", "Another book"));
    [Fact] void should_surface_the_current_observation_limitation() => _observation.Initial.HasExceptions.ShouldBeTrue();
    [Fact] void should_preserve_chronicles_parent_key_error() => _observation.Initial.ExceptionMessages.Single().StartsWith("Could not resolve parent key for event 'ObservedBookAdded'", StringComparison.Ordinal).ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
