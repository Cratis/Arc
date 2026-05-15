// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions.when_getting_event_source_id;

public class from_subclass_of_generic_event_source_id_in_response : given.a_command_context
{
    TypedId _typedEventSourceId;
    EventSourceId _result;

    void Establish()
    {
        _typedEventSourceId = new(Guid.NewGuid());
        _commandContext = _commandContext with { Response = _typedEventSourceId };
    }

    void Because() => _result = _commandContext.GetEventSourceId();

    [Fact] void should_return_the_event_source_id_from_response() => _result.ShouldEqual((EventSourceId)_typedEventSourceId);

    record TypedId(Guid Value) : EventSourceId<Guid>(Value);
}