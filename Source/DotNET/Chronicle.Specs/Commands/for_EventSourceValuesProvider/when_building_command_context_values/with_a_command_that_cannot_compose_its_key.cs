// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_building_command_context_values;

/// <summary>
/// Exercises the real <see cref="CommandContextValuesBuilder"/> rather than the provider alone, because that is the frame
/// the failure actually escapes through: context values are built before the filter chain, so an exception here bypasses
/// the per-filter handling that turns bad input into a clean response and surfaces as an unhandled server error instead.
/// </summary>
public class with_a_command_that_cannot_compose_its_key : Specification
{
    CommandContextValuesBuilder _builder;
    CommandContextValues _result;
    Exception _exception;

    void Establish() => _builder = new CommandContextValuesBuilder(
        new KnownInstancesOf<ICommandContextValuesProvider>([new EventSourceValuesProvider()]));

    void Because() => _exception = Catch.Exception(() => _result = _builder.Build(new CommandThatCannotComposeItsKey(null!)));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_still_build_an_event_source_id() =>
        ((EventSourceId)_result[WellKnownCommandContextKeys.EventSourceId]).ShouldEqual(EventSourceId.Unspecified);

    record CommandThatCannotComposeItsKey(EventSourceId<Guid> Id) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => Id;
    }
}
