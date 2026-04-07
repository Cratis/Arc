// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_with_generic_result;

public class and_authorization_fails : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;

    void Establish() =>
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(Task.FromResult(CommandResult.Unauthorized(_correlationId)));

    async Task Because() => _result = await _commandPipeline.Execute<string>(_command, _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_null_response() => _result.Response.ShouldBeNull();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_call_command_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
