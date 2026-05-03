// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_command_with_generic_event_source_id_property : Specification
{
    CommandWithGenericEventSourceId _command;
    EventSourceId<Guid> _typedEventSourceId;
    EventSourceId _result;

    void Establish()
    {
        _typedEventSourceId = new EventSourceId<Guid>(Guid.NewGuid());
        _command = new(_typedEventSourceId);
    }

    void Because() => _result = _command.GetEventSourceId();

    [Fact] void should_return_the_event_source_id() => _result.ShouldEqual((EventSourceId)_typedEventSourceId);

    record CommandWithGenericEventSourceId(EventSourceId<Guid> EventSourceId);
}
