// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions.when_checking_has_event_source_id;

public class with_subclass_of_generic_event_source_id_in_response : given.a_command_context
{
    bool _result;

    void Establish() => _commandContext = _commandContext with { Response = new TypedId(Guid.NewGuid()) };

    void Because() => _result = _commandContext.HasEventSourceId();

    [Fact] void should_return_true() => _result.ShouldBeTrue();

    record TypedId(Guid Value) : EventSourceId<Guid>(Value);
}