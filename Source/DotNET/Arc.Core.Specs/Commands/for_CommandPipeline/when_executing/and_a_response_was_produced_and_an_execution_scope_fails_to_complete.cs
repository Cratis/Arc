// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_response_was_produced_and_an_execution_scope_fails_to_complete : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    string _response;
    Exception _failure;

    void Establish()
    {
        _response = "Forty two";
        _failure = new Exception("Commit failed");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_response);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _response).Returns(false);
        _executionScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Throws(_failure);
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<string>;

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_carry_the_response() => _result.Response.ShouldBeNull();
    [Fact] void should_keep_the_exception_message() => _result.ExceptionMessages.ShouldContain(_failure.Message);
    [Fact] void should_keep_the_exception_stack_trace() => _result.ExceptionStackTrace.ShouldNotBeEmpty();
    [Fact] void should_keep_the_result_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_invent_validation_results() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_complete_the_scope_exactly_once() => _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
}
