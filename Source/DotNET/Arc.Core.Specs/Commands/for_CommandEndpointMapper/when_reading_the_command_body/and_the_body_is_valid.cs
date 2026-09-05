// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_reading_the_command_body;

public class and_the_body_is_valid : given.a_command_endpoint_mapper
{
    SomeCommand _deserializedCommand;
    CommandResult? _failure;
    object? _command;

    void Establish()
    {
        _deserializedCommand = new SomeCommand(42, "ok");
        _context.ReadBodyAsJson(_commandType, Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(_deserializedCommand));
    }

    async Task Because() => (_command, _failure) = await CommandEndpointMapper.ReadCommandBody(_context, _commandType, _correlationId, _logger);

    [Fact] void should_return_the_deserialized_command() => _command.ShouldEqual(_deserializedCommand);
    [Fact] void should_not_produce_a_failure() => _failure.ShouldBeNull();
}
