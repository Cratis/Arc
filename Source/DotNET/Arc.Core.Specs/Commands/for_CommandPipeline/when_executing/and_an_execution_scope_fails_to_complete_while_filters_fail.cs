// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_an_execution_scope_fails_to_complete_while_filters_fail : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish()
    {
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Error(CorrelationId.New(), "Not successful"));
        _executionScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Throws(new Exception("Rollback failed"));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_fold_the_failure_into_the_result() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_complete_the_scope_exactly_once() => _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
}
