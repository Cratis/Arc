// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootServiceCollectionExtensions.when_resolving_without_dynamic_code;

public class and_the_factory_returns_the_aggregate_root : Specification
{
    IAggregateRootFactory _factory;
    SampleAggregateRoot _aggregateRoot;
    EventSourceId _eventSourceId;
    object _result;

    void Establish()
    {
        _aggregateRoot = new SampleAggregateRoot();
        _eventSourceId = EventSourceId.New();
        _factory = Substitute.For<IAggregateRootFactory>();
        _factory.Get<SampleAggregateRoot>(_eventSourceId, null, null).Returns(Task.FromResult(_aggregateRoot));
    }

    void Because() => _result = AggregateRootServiceCollectionExtensions.GetAggregateRootWithoutDynamicCode(typeof(SampleAggregateRoot), _factory, _eventSourceId);

    [Fact] void should_return_the_aggregate_root_itself() => _result.ShouldEqual(_aggregateRoot);
    [Fact] void should_ask_the_factory_for_the_event_source() => _factory.Received(1).Get<SampleAggregateRoot>(_eventSourceId, null, null);
}
