// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_tuple_command_containing_generic_event_source_id : Specification
{
    ITuple _command;
    EventSourceId<Guid> _typedEventSourceId;
    EventSourceId _result;

    void Establish()
    {
        _typedEventSourceId = new EventSourceId<Guid>(Guid.NewGuid());
        _command = (_typedEventSourceId, "some-data", 42);
    }

    void Because() => _result = _command.GetEventSourceId();

    [Fact] void should_return_the_event_source_id() => _result.ShouldEqual((EventSourceId)_typedEventSourceId);
}
