// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class with_multiple_successful_commands : given.a_command_side_effect_executor
{
    TestCommand _first;
    TestCommand _second;
    Result<ReactorSideEffectFailure> _result;

    void Establish()
    {
        _first = new TestCommand("first");
        _second = new TestCommand("second");
        _commandPipeline.Execute(_first, _serviceProvider).Returns(CommandResult.Success(CorrelationId.New()));
        _commandPipeline.Execute(_second, _serviceProvider).Returns(CommandResult.Success(CorrelationId.New()));
    }

    async Task Because() => _result = await _executor.Execute([_first, _second], _reactorType);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_create_a_single_service_scope() => _serviceScopeFactory.Received(1).CreateScope();
    [Fact] void should_execute_the_first_command() => _commandPipeline.Received(1).Execute(_first, _serviceProvider);
    [Fact] void should_execute_the_second_command() => _commandPipeline.Received(1).Execute(_second, _serviceProvider);
    [Fact] void should_dispose_the_scope() => _serviceScope.Received(1).Dispose();
}
