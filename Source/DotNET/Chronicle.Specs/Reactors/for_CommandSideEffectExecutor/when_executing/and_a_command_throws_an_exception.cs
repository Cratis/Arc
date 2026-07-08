// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class and_a_command_throws_an_exception : given.a_command_side_effect_executor
{
    TestCommand _command;
    Result<ReactorSideEffectFailure> _result;
    ReactorSideEffectFailure _failure;

    void Establish()
    {
        _command = new TestCommand("Test");
        _commandPipeline.Execute(_command, _serviceProvider).Returns(CommandResult.Error(CorrelationId.New(), "Something went wrong"));
    }

    async Task Because()
    {
        _result = await _executor.Execute([_command]);
        _result.TryGetError(out _failure);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_describe_the_command_that_failed() => _failure.GetMessages().Any(_ => _.Contains(nameof(TestCommand))).ShouldBeTrue();
    [Fact] void should_include_the_exception_message() => _failure.GetMessages().Any(_ => _.Contains("Something went wrong")).ShouldBeTrue();
}
