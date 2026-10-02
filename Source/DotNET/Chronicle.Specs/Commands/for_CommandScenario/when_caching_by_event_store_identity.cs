// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_caching_by_event_store_identity : Specification
{
    CommandScenario<UseReadModelDependencyCommand> _firstScenario;
    CommandScenario<UseReadModelDependencyCommand> _secondScenario;
    IEventStore _firstStore;
    IEventStore _secondStore;
    Dictionary<(string Store, string Namespace), int> _cachedRevisions;

    void Establish()
    {
        _firstScenario = new();
        _secondScenario = new();
        _firstStore = (IEventStore)_firstScenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        _secondStore = (IEventStore)_secondScenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        _cachedRevisions = new()
        {
            [(_firstStore.Name.Value, _firstStore.Namespace.Value)] = 1
        };
    }

    void Because() => _cachedRevisions[(_secondStore.Name.Value, _secondStore.Namespace.Value)] = 0;

    async Task Destroy()
    {
        await _firstScenario.DisposeAsync();
        await _secondScenario.DisposeAsync();
    }

    [Fact] void should_keep_separate_cache_entries() => _cachedRevisions.Count.ShouldEqual(2);
    [Fact] void should_not_overwrite_the_first_scenarios_revision() => _cachedRevisions[(_firstStore.Name.Value, _firstStore.Namespace.Value)].ShouldEqual(1);
    [Fact] void should_cache_the_second_scenarios_own_revision() => _cachedRevisions[(_secondStore.Name.Value, _secondStore.Namespace.Value)].ShouldEqual(0);
}
