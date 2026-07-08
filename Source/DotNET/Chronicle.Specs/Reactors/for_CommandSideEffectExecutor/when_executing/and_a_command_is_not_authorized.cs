// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class and_a_command_is_not_authorized : given.a_command_side_effect_executor
{
    TestCommand _command;
    Result<ReactorSideEffectFailure> _result;
    ReactorSideEffectFailure _failure;

    void Establish()
    {
        _command = new TestCommand("Test");
        _commandPipeline.Execute(_command, _serviceProvider).Returns(CommandResult.Unauthorized(CorrelationId.New(), "not allowed"));
    }

    async Task Because()
    {
        _result = await _executor.Execute([_command]);
        _result.TryGetError(out _failure);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_produce_a_single_append_failure() => _failure.AppendFailures.Count().ShouldEqual(1);
    [Fact] void should_describe_the_command_that_failed() => _failure.GetMessages().Any(_ => _.Contains(nameof(TestCommand))).ShouldBeTrue();
    [Fact] void should_describe_the_authorization_failure() => _failure.GetMessages().Any(_ => _.Contains("not authorized")).ShouldBeTrue();
}
