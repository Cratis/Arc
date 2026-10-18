// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_two_decision_scenarios_are_created : Specification
{
    CommandScenario<when_automatically_enabling_decision_reads.Ordinary> _firstScenario;
    CommandScenario<when_automatically_enabling_decision_reads.AutomaticRead> _secondScenario;
    IEventStore _firstStore;
    IEventStore _secondStore;
    EventStoreName _originalName;
    EventStoreNamespaceName _originalNamespace;
    AppendResult _appendResult;
    AppendedEvent _storedEvent;

    void Establish()
    {
        _firstScenario = new();
        var legacyStore = (IEventStore)_firstScenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        _originalName = legacyStore.Name;
        _originalNamespace = legacyStore.Namespace;
    }

    async Task Because()
    {
        _firstScenario.UseDecisionReads();
        _secondScenario = new();
        _firstStore = (IEventStore)_firstScenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        _secondStore = (IEventStore)_secondScenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        var source = EventSourceId.New();
        _appendResult = await _firstScenario.EventLog.Append(source, new when_using_decision_mode.DecisionStateChanged());
        _storedEvent = (await _firstStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, source)).Single();
    }

    async Task Destroy()
    {
        await _firstScenario.DisposeAsync();
        await _secondScenario.DisposeAsync();
    }

    [Fact] void should_have_distinct_event_store_names() => _firstStore.Name.ShouldNotEqual(_secondStore.Name);
    [Fact] void should_have_distinct_namespaces() => _firstStore.Namespace.ShouldNotEqual(_secondStore.Namespace);
    [Fact] void should_keep_the_generated_event_store_name() => _firstStore.Name.ShouldEqual(_originalName);
    [Fact] void should_keep_the_generated_namespace() => _firstStore.Namespace.ShouldEqual(_originalNamespace);
    [Fact] void should_append_successfully() => _appendResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_event_stores_name_in_the_append_result() => _appendResult.EventStore.ShouldEqual(_firstStore.Name);
    [Fact] void should_report_the_event_stores_namespace_in_the_append_result() => _appendResult.EventStoreNamespace.ShouldEqual(_firstStore.Namespace);
    [Fact] void should_keep_the_event_store_name_in_the_event_context() => _storedEvent.Context.EventStore.ShouldEqual(_firstStore.Name);
    [Fact] void should_keep_the_namespace_in_the_event_context() => _storedEvent.Context.Namespace.ShouldEqual(_firstStore.Namespace);
}
