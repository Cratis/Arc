// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_without_service_provider;

public class and_executing_generic : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    string _response;

    void Establish()
    {
        _response = "Forty two";
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_response);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _response).Returns(false);
    }

    async Task Because() => _result = await _commandPipeline.Execute<string>(_command);

    [Fact] void should_create_a_scope() => _serviceScopeFactory.Received(1).CreateScope();
    [Fact] void should_dispose_the_scope() => _serviceScope.Received(1).Dispose();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result.Response.ShouldEqual(_response);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
