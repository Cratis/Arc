// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_appending_with_the_scenario_identity : Specification
{
    CommandScenario<StartPartnerOnboardingDirect> _scenario;
    IEventStore _store;
    IEventLog _eventLog;
    EventSourceId _source;
    AppendResult _result;
    AppendedEvent _storedEvent;

    void Establish()
    {
        _scenario = new();
        _store = (IEventStore)_scenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        _eventLog = (IEventLog)_scenario.Services.Single(_ => _.ServiceType == typeof(IEventLog)).ImplementationInstance!;
        _source = EventSourceId.New();
    }

    async Task Because()
    {
        _result = await _eventLog.Append(_source, new PartnerOnboardingStarted("ORG-123"));
        _storedEvent = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source)).Single();
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_scenarios_event_store_name() => _result.EventStore.ShouldEqual(_store.Name);
    [Fact] void should_return_the_scenarios_namespace() => _result.EventStoreNamespace.ShouldEqual(_store.Namespace);
    [Fact] void should_store_the_event_in_the_scenarios_event_store() => _storedEvent.Context.EventStore.ShouldEqual(_store.Name);
    [Fact] void should_store_the_event_in_the_scenarios_namespace() => _storedEvent.Context.Namespace.ShouldEqual(_store.Namespace);
}
