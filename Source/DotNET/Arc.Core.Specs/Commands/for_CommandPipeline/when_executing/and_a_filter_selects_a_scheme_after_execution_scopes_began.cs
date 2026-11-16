// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_filter_selects_a_scheme_after_execution_scopes_began : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
    {
        call.Arg<CommandContext>().AuthorizedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([], "late"));
        return CommandResult.Success(_correlationId);
    });

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_report_an_authorization_configuration_failure() => _result.AuthorizationConfigurationFailed.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_resolve_arguments() => _commandHandlerArgumentResolver.DidNotReceiveWithAnyArgs().Resolve(default!, default!, default!, default);
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
    [Fact] void should_complete_the_scope_exactly_once() => _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
}
