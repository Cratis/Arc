// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_model_bound_children_in_decision_mode : Specification
{
    readonly CommandScenario<ChangeObservedAccount> _commands = new();
    readonly QueryScenario<ObservedShelf> _scenario = new();
    readonly EventSourceId _id = EventSourceId.New();
    ObservedShelf _observed = default!;
    ObservedShelf _resolved = default!;

    void Establish()
    {
        foreach (var resource in _scenario.Context.Values.Distinct().OfType<IDisposable>())
        {
            resource.Dispose();
        }
        _scenario.Context.Clear();
        _scenario.Services.Clear();
        foreach (var service in _commands.Services)
        {
            _scenario.Services.Add(service);
        }
        foreach (var (key, value) in _commands.Context)
        {
            _scenario.Context[key] = value;
        }
        _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(42m), new ObservedBookAdded("first", "A book"), new ObservedBookAdded("second", "Another book"));
    }

    async Task Because()
    {
        await using var services = _scenario.Services.BuildServiceProvider();
        _resolved = await services.GetRequiredService<IReadModels>().GetInstanceById<ObservedShelf>(new ReadModelKey(_id.Value));
        var observation = await _scenario.Observe(nameof(ObservedShelf.All));
        Assert.True(observation.Initial.IsSuccess, string.Join("; ", observation.Initial.ExceptionMessages));
        _observed = ((IEnumerable<ObservedShelf>)observation.Initial.Data).Single();
    }

    [Fact] void should_use_the_same_parent_key_as_instance_reads() => _observed.ShelfId.ShouldEqual(_resolved.ShelfId);
    [Fact] void should_project_the_parent_state() => _observed.Balance.ShouldEqual(42m);
    [Fact] void should_project_both_children() => _observed.Books.ShouldContainOnly(new ObservedBook("first", "A book"), new ObservedBook("second", "Another book"));
    [Fact] void should_match_instance_reads() => _observed.Books.ShouldContainOnly(_resolved.Books);

    void Destroy()
    {
        _scenario.Dispose();
        _commands.Dispose();
    }
}
