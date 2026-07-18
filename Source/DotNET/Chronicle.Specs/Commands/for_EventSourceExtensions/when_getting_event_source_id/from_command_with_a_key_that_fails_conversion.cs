// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_command_with_a_key_that_fails_conversion : Specification
{
    CommandWithFailingKey _command;
    EventSourceId _result;
    Exception _exception;

    void Establish() => _command = new(new FailingKey());

    void Because() => _exception = Catch.Exception(() => _result = _command.GetEventSourceId());

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_return_unspecified() => _result.ShouldEqual(EventSourceId.Unspecified);

    public class FailingKey
    {
        public override string ToString() => throw new InvalidOperationException("the underlying key value is null");
    }

    public class CommandWithFailingKey(FailingKey key)
    {
        [Key]
        public FailingKey Key { get; init; } = key;
    }
}
