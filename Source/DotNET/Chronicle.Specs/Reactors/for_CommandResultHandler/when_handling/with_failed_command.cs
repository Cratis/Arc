// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandResultHandler.when_handling;

public class with_failed_command : given.a_command_result_handler
{
    TestCommand _command;
    CommandResult _commandResult;
    Exception _exception;

    void Establish()
    {
        _command = new TestCommand("Test");
        _commandResult = new CommandResult
        {
            CorrelationId = CorrelationId.New(),
            ExceptionMessages = ["Command failed"]
        };
        _commandPipeline.Execute(_command, _serviceProvider).Returns(_commandResult);
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _handler.Handle(_reactorContext, _eventStore, _command));

    [Fact] void should_throw_command_execution_failed_exception() => _exception.ShouldBeOfExactType<CommandExecutionFailedException>();
    [Fact] void should_include_command_type_in_exception() => (_exception as CommandExecutionFailedException)!.CommandType.ShouldEqual(typeof(TestCommand));
    [Fact] void should_include_command_result_in_exception() => (_exception as CommandExecutionFailedException)!.Result.ShouldEqual(_commandResult);
}
