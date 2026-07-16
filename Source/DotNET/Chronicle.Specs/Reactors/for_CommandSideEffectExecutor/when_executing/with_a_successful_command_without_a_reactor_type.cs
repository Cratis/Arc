// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class with_a_successful_command_without_a_reactor_type : given.a_command_side_effect_executor
{
    TestCommand _command;
    Result<ReactorSideEffectFailure> _result;

    void Establish()
    {
        _command = new TestCommand("Test");
        _commandPipeline.Execute(_command, _serviceProvider).Returns(CommandResult.Success(CorrelationId.New()));
    }

    async Task Because() => _result = await _executor.Execute([_command]);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_execute_the_command_through_the_pipeline() => _commandPipeline.Received(1).Execute(_command, _serviceProvider);
    [Fact] void should_not_establish_a_system_execution_scope() => _systemExecution.DidNotReceive().AsSystem(Arg.Any<string[]>());
}
