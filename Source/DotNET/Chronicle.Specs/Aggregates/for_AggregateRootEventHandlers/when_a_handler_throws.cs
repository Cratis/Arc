// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootEventHandlers;

public class when_a_handler_throws : given.aggregate_root_event_handlers_for<AggregateRootWithThrowingHandler>
{
    AggregateRootWithThrowingHandler _aggregateRoot;
    Exception _exception;

    protected override IEnumerable<Type> GetEventTypes() => [typeof(ThrowingEvent)];

    void Establish() => _aggregateRoot = new AggregateRootWithThrowingHandler();

    async Task Because() => _exception = await Catch.Exception(() => handlers.Handle(_aggregateRoot, [new EventAndContext(new ThrowingEvent(), EventContext.Empty)]));

    [Fact] void should_surface_the_real_exception_not_the_reflection_wrapper() => _exception.ShouldBeOfExactType<AggregateRootHandlerFailed>();
}
