// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceDefinitionValuesProvider.when_providing;

public class with_a_declared_event_source : Specification
{
    EventSourceDefinitionValuesProvider _provider;
    CommandContextValues _result;

    void Establish()
    {
        var eventSources = Substitute.For<IEventSources>();
        eventSources.GetFor(typeof(Account)).Returns(new EventSourceDefinition(typeof(Account), "account", "", ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType, [new("transactions", "", ConcurrencyDimensions.None)]));
        _provider = new(eventSources);
    }

    void Because() => _result = _provider.Provide(new RecordTransaction());

    [Fact] void should_keep_the_event_source_definition() => _result[WellKnownCommandContextKeys.EventSource].ShouldEqual(typeof(Account));
    [Fact] void should_resolve_the_event_source_type() => _result[WellKnownCommandContextKeys.EventSourceType].ShouldEqual(new Cratis.Chronicle.Events.EventSourceType("account"));
    [Fact] void should_resolve_the_declared_stream_type() => _result[WellKnownCommandContextKeys.EventStreamType].ShouldEqual(new Cratis.Chronicle.Events.EventStreamType("transactions"));
    [Fact] void should_keep_the_declared_concurrency_dimensions() => _result[WellKnownCommandContextKeys.ConcurrencyDimensions].ShouldEqual(ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType);

    [EventSource<Account>("transactions")]
    record RecordTransaction;

    [Cratis.Chronicle.EventSources.EventSource]
    [EventStream("transactions")]
    class Account : IEventSource;
}
