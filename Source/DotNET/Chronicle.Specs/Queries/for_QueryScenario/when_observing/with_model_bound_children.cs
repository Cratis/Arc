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
    ObservedShelf _observed = default!;
    ObservedShelf _resolved = default!;

    void Establish() => _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(42m), new ObservedBookAdded("first", "A book"), new ObservedBookAdded("second", "Another book"));

    async Task Because()
    {
        await using var services = _scenario.Services.BuildServiceProvider();
        _resolved = await services.GetRequiredService<IReadModels>().GetInstanceById<ObservedShelf>(new ReadModelKey(_id.Value));

        var observation = await _scenario.Observe(nameof(ObservedShelf.All));
        Assert.True(observation.Initial.IsSuccess, string.Join("; ", observation.Initial.ExceptionMessages));
        _observed = ((IEnumerable<ObservedShelf>)observation.Initial.Data).Single();
    }

    [Fact] void should_project_the_parent_in_instance_reads() => _resolved.Balance.ShouldEqual(42m);
    [Fact] void should_project_children_in_instance_reads() => _resolved.Books.ShouldContainOnly(new ObservedBook("first", "A book"), new ObservedBook("second", "Another book"));
    [Fact] void should_emit_the_same_parent_key_as_instance_reads() => _observed.ShelfId.ShouldEqual(_resolved.ShelfId);
    [Fact] void should_emit_the_parent_state() => _observed.Balance.ShouldEqual(42m);
    [Fact] void should_emit_both_children() => _observed.Books.ShouldContainOnly(new ObservedBook("first", "A book"), new ObservedBook("second", "Another book"));

    void Destroy() => _scenario.Dispose();
}
