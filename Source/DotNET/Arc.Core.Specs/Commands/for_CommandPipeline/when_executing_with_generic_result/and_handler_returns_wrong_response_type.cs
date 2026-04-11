// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_with_generic_result;

public class and_handler_returns_wrong_response_type : given.a_command_pipeline_and_a_handler_for_command
{
    Exception _thrownException;
    int _intResponse;

    void Establish()
    {
        _intResponse = 42;
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_intResponse);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _intResponse).Returns(false);
    }

    async Task Because()
    {
        try
        {
            await _commandPipeline.Execute<string>(_command, _serviceProvider);
        }
        catch (Exception ex)
        {
            _thrownException = ex;
        }
    }

    [Fact] void should_throw_invalid_cast_exception() => _thrownException.ShouldBeOfExactType<InvalidCastException>();
}
