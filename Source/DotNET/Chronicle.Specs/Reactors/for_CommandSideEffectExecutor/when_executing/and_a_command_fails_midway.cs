// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class and_a_command_fails_midway : given.a_command_side_effect_executor
{
    TestCommand _first;
    TestCommand _second;
    TestCommand _third;
    Result<ReactorSideEffectFailure> _result;
    ReactorSideEffectFailure _failure;

    void Establish()
    {
        _first = new TestCommand("first");
        _second = new TestCommand("second");
        _third = new TestCommand("third");
        _commandPipeline.Execute(_first, _serviceProvider).Returns(CommandResult.Success(CorrelationId.New()));
        _commandPipeline.Execute(_second, _serviceProvider).Returns(CommandResult.Error(CorrelationId.New(), "boom"));
        _commandPipeline.Execute(_third, _serviceProvider).Returns(CommandResult.Success(CorrelationId.New()));
    }

    async Task Because()
    {
        _result = await _executor.Execute([_first, _second, _third]);
        _result.TryGetError(out _failure);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_execute_the_first_command() => _commandPipeline.Received(1).Execute(_first, _serviceProvider);
    [Fact] void should_execute_the_failing_command() => _commandPipeline.Received(1).Execute(_second, _serviceProvider);
    [Fact] void should_not_execute_the_command_after_the_failure() => _commandPipeline.DidNotReceive().Execute(_third, _serviceProvider);
    [Fact] void should_report_the_failure_from_the_failing_command() => _failure.GetMessages().Any(_ => _.Contains("boom")).ShouldBeTrue();
    [Fact] void should_dispose_the_scope() => _serviceScope.Received(1).Dispose();
}
