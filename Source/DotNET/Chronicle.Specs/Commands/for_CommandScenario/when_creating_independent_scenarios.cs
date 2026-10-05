// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_creating_independent_scenarios : Specification
{
    CommandScenario<UseReadModelDependencyCommand> _firstScenario;
    CommandScenario<UseReadModelDependencyCommand> _secondScenario;
    IEventStore _firstStore;
    IEventStore _secondStore;
    EventStoreName _firstName;
    EventStoreNamespaceName _firstNamespace;
    IEnumerable<EventStoreNamespaceName> _firstNamespaces;
    IEnumerable<EventStoreNamespaceName> _secondNamespaces;

    void Establish()
    {
        _firstScenario = new();
        _firstStore = (IEventStore)_firstScenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        _firstName = _firstStore.Name;
        _firstNamespace = _firstStore.Namespace;
    }

    async Task Because()
    {
        _secondScenario = new();
        _secondStore = (IEventStore)_secondScenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        _firstNamespaces = await _firstStore.GetNamespaces();
        _secondNamespaces = await _secondStore.GetNamespaces();
    }

    async Task Destroy()
    {
        await _firstScenario.DisposeAsync();
        await _secondScenario.DisposeAsync();
    }

    [Fact] void should_have_distinct_event_store_names() => (_firstStore.Name == _secondStore.Name).ShouldBeFalse();
    [Fact] void should_have_distinct_namespaces() => (_firstStore.Namespace == _secondStore.Namespace).ShouldBeFalse();
    [Fact] void should_keep_the_event_store_name_stable() => _firstStore.Name.ShouldEqual(_firstName);
    [Fact] void should_keep_the_namespace_stable() => _firstStore.Namespace.ShouldEqual(_firstNamespace);
    [Fact] void should_list_only_the_first_scenarios_namespace() => _firstNamespaces.ShouldContainOnly(_firstStore.Namespace);
    [Fact] void should_list_only_the_second_scenarios_namespace() => _secondNamespaces.ShouldContainOnly(_secondStore.Namespace);
}
