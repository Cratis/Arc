// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootEventHandlers.when_creating_for;

public class with_an_explicitly_registered_event_without_attribute : Specification
{
    IEventTypes _eventTypes;
    EventType _registeredEventType;
    AggregateRootEventHandlers _handlers;

    void Establish()
    {
        _registeredEventType = new EventType("explicitly-registered", 1);
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.AllClrTypes.Returns(ImmutableList.Create(typeof(RegisteredEvent)));
        _eventTypes.GetEventTypeFor(typeof(RegisteredEvent)).Returns(_registeredEventType);
    }

    void Because() => _handlers = new AggregateRootEventHandlers(_eventTypes, typeof(RegisteredAggregate));

    [Fact] void should_discover_the_handler() => _handlers.HasHandleMethods.ShouldBeTrue();
    [Fact] void should_use_the_registered_event_type() => _handlers.EventTypes.ShouldContainOnly(_registeredEventType);

    record RegisteredEvent;

    sealed class RegisteredAggregate : AggregateRoot
    {
        public Task On(RegisteredEvent @event) => Task.CompletedTask;
    }
}
