// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_command_has_a_provide_method : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    async Task Because() => _result = await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_resolve_arguments() => _commandHandlerArgumentResolver.DidNotReceive().Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>());
    [Fact] void should_not_call_command_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
