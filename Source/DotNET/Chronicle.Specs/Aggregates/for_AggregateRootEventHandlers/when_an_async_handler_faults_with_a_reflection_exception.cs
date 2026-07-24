// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootEventHandlers;

public class when_an_async_handler_faults_with_a_reflection_exception : given.aggregate_root_event_handlers_for<AggregateRootWithAsyncFaultingHandler>
{
    AggregateRootWithAsyncFaultingHandler _aggregateRoot;
    Exception _exception;

    protected override IEnumerable<Type> GetEventTypes() => [typeof(AsyncFaultingEvent)];

    void Establish() => _aggregateRoot = new AggregateRootWithAsyncFaultingHandler();

    async Task Because() => _exception = await Catch.Exception(() => handlers.Handle(_aggregateRoot, [new EventAndContext(new AsyncFaultingEvent(), EventContext.Empty)]));

    [Fact] void should_surface_the_handlers_own_exception_intact() => _exception.ShouldBeOfExactType<TargetInvocationException>();
}
