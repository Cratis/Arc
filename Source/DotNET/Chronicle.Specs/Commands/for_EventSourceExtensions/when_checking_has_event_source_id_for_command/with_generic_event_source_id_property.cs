// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceExtensions.when_checking_has_event_source_id_for_command;

public class with_generic_event_source_id_property : Specification
{
    CommandWithGenericEventSourceId _command;
    bool _result;

    void Establish() => _command = new(new EventSourceId<Guid>(Guid.NewGuid()));

    void Because() => _result = _command.HasEventSourceId();

    [Fact] void should_return_true() => _result.ShouldBeTrue();

    record CommandWithGenericEventSourceId(EventSourceId<Guid> EventSourceId);
}
