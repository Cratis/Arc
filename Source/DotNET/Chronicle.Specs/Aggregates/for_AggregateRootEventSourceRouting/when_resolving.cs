// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootEventSourceRouting;

public class when_resolving : Specification
{
    IEventSources _eventSources;
    AggregateRootEventSourceRouting _declared;
    AggregateRootEventSourceRouting _streamless;
    AggregateRootEventSourceRouting _undeclared;
    Exception _unknownStream;
    Exception _contradictingSourceType;
    Exception _contradictingStreamType;

    void Establish()
    {
        _eventSources = Substitute.For<IEventSources>();
        _eventSources.GetFor(typeof(WalletSource)).Returns(new EventSourceDefinition(typeof(WalletSource), "WalletSource", "", ConcurrencyDimensions.None, [new("transactions", "", ConcurrencyDimensions.None)]));
    }

    void Because()
    {
        _declared = AggregateRootEventSourceRouting.Resolve(typeof(Declared), () => _eventSources);
        _streamless = AggregateRootEventSourceRouting.Resolve(typeof(Streamless), () => _eventSources);
        _undeclared = AggregateRootEventSourceRouting.Resolve(typeof(Undeclared), () => _eventSources);
        _unknownStream = Catch.Exception(() => AggregateRootEventSourceRouting.Resolve(typeof(UnknownStream), () => _eventSources));
        _contradictingSourceType = Catch.Exception(() => AggregateRootEventSourceRouting.Resolve(typeof(Declared), () => _eventSources, new EventSourceType("other")));
        _contradictingStreamType = Catch.Exception(() => AggregateRootEventSourceRouting.Resolve(typeof(ContradictingStreamType), () => _eventSources));
    }

    [Fact] void should_resolve_the_source_type() => _declared.EventSourceType.ShouldEqual(new EventSourceType("WalletSource"));
    [Fact] void should_resolve_the_stream_type() => _declared.EventStreamType.ShouldEqual(new EventStreamType("transactions"));
    [Fact] void should_keep_the_stream_name() => _declared.EventStream.ShouldEqual("transactions");
    [Fact] void should_fall_back_to_the_aggregate_name_without_a_stream() => _streamless.EventStreamType.ShouldEqual(new EventStreamType(nameof(Streamless)));
    [Fact] void should_have_no_stream_name_without_a_stream() => _streamless.EventStream.ShouldBeNull();
    [Fact] void should_resolve_nothing_without_a_declaration() => _undeclared.ShouldBeNull();
    [Fact] void should_reject_a_stream_the_source_does_not_declare() => _unknownStream.ShouldBeOfExactType<AggregateRootContradictsEventSource>();
    [Fact] void should_reject_a_contradicting_requested_source_type() => _contradictingSourceType.ShouldBeOfExactType<AggregateRootContradictsEventSource>();
    [Fact] void should_reject_a_contradicting_stream_type_attribute() => _contradictingStreamType.ShouldBeOfExactType<AggregateRootContradictsEventSource>();

    [EventSource<WalletSource>("transactions")]
    class Declared : AggregateRoot;

    [EventSource<WalletSource>]
    class Streamless : AggregateRoot;

    class Undeclared : AggregateRoot;

    [EventSource<WalletSource>("missing")]
    class UnknownStream : AggregateRoot;

    [EventSource<WalletSource>("transactions")]
    [EventStreamType("other")]
    class ContradictingStreamType : AggregateRoot;

    [Cratis.Chronicle.EventSources.EventSource]
    [EventStream("transactions")]
    class WalletSource : IEventSource;
}
