// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

/// <summary>
/// A command can hand back a null key without throwing — a null-tolerant implementation returning the value it was given.
/// The context value must still be a usable event source id rather than a null the rest of the pipeline dereferences.
/// </summary>
public class with_a_command_that_provides_a_null_key : Specification
{
    EventSourceValuesProvider _provider;
    CommandContextValues _result;
    Exception _exception;

    void Establish() => _provider = new EventSourceValuesProvider();

    void Because() => _exception = Catch.Exception(() => _result = _provider.Provide(new CommandThatProvidesNull()));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_provide_an_unspecified_event_source_id() =>
        ((EventSourceId)_result[WellKnownCommandContextKeys.EventSourceId]).ShouldEqual(EventSourceId.Unspecified);

    record CommandThatProvidesNull : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => null!;
    }
}
