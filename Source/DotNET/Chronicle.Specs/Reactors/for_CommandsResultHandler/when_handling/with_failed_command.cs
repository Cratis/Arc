// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandsResultHandler.when_handling;

public class with_failed_command : given.a_commands_result_handler
{
    TestCommand _command1;
    TestCommand _command2;
    CommandResult _failedResult;
    Exception _exception = null!;
    CorrelationId _correlationId1;
    CorrelationId _correlationId2;

    void Establish()
    {
        _command1 = new TestCommand("cmd1");
        _command2 = new TestCommand("cmd2");
        _correlationId1 = CorrelationId.New();
        _correlationId2 = CorrelationId.New();

        _commandPipeline.Execute(_command1, _serviceProvider).Returns(CommandResult.Success(_correlationId1));
        _failedResult = new CommandResult
        {
            CorrelationId = _correlationId2,
            ValidationResults = [new ValidationResult(ValidationResultSeverity.Error, "Command failed", [], null)]
        };
        _commandPipeline.Execute(_command2, _serviceProvider).Returns(_failedResult);
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _handler.Handle(_reactorContext, _eventStore, new[] { _command1, _command2 }));

    [Fact] void should_throw_command_execution_failed_exception() => _exception.ShouldBeOfExactType<CommandExecutionFailedException>();
    [Fact] void should_have_command_type_in_exception() => ((CommandExecutionFailedException)_exception).CommandType.ShouldEqual(typeof(TestCommand));
    [Fact] void should_have_command_result_in_exception() => ((CommandExecutionFailedException)_exception).Result.ShouldEqual(_failedResult);
    [Fact] void should_dispose_the_scope() => _serviceScope.Received(1).Dispose();
}
