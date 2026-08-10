// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_with_generic_result;

/// <summary>
/// Asking for a base type the response is assignable to re-wraps the result into CommandResult&lt;TResult&gt;. The
/// re-wrap copies the response across, so it must not resurrect one the pipeline already took back from a failure.
/// </summary>
public class and_a_response_was_produced_and_an_execution_scope_fails_to_complete : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<IAnimal> _result;
    Dog _dog;
    Exception _failure;

    void Establish()
    {
        _dog = new Dog("Rex");
        _failure = new Exception("Commit failed");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_dog);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _dog).Returns(false);
        _executionScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Throws(_failure);
    }

    async Task Because() => _result = await _commandPipeline.Execute<IAnimal>(_command, _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_no_response() => _result.Response.ShouldBeNull();
    [Fact] void should_keep_the_exception_message() => _result.ExceptionMessages.ShouldContain(_failure.Message);

    interface IAnimal;

    record Dog(string Name) : IAnimal;
}
