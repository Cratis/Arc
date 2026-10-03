// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory;

public class when_getting_aggregate_root_with_a_declared_event_source : given.an_aggregate_root_factory
{
    AccountAggregate _result;

    void Establish()
    {
        var eventSources = Substitute.For<IEventSources>();
        eventSources.GetFor(typeof(LedgerSource)).Returns(new EventSourceDefinition(typeof(LedgerSource), "LedgerSource", "", ConcurrencyDimensions.None, [new("transactions", "", ConcurrencyDimensions.None)]));
        _mutatorFactory.Create<AccountAggregate>(Arg.Any<AggregateRootContext>()).Returns(_mutator);
        _serviceProvider.GetService(typeof(IEventSources)).Returns(eventSources);
    }

    async Task Because() => _result = await _factory.Get<AccountAggregate>(EventSourceId.New());

    [Fact] void should_use_the_event_source_type_of_the_definition() => _result._context.EventSourceType.ShouldEqual(new EventSourceType("LedgerSource"));
    [Fact] void should_use_the_declared_stream_as_stream_type() => _result._context.EventStreamType.ShouldEqual(new EventStreamType("transactions"));
    [Fact] void should_keep_the_event_source_on_the_context() => ((IAggregateRootEventSourceContext)_result._context).EventSource.ShouldEqual(typeof(LedgerSource));
    [Fact] void should_keep_the_declared_stream_on_the_context() => ((IAggregateRootEventSourceContext)_result._context).EventStream.ShouldEqual("transactions");

    [EventSource<LedgerSource>("transactions")]
    public class AccountAggregate : AggregateRoot;

    [Cratis.Chronicle.EventSources.EventSource]
    [EventStream("transactions")]
    class LedgerSource : IEventSource;
}
