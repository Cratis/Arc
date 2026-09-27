// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_authorization_declarations_are_unavailable : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    async Task Because() => _result = await _commandPipeline.Execute(_command, Substitute.For<IServiceProvider>());

    [Fact] void should_not_report_success_without_knowing_the_effective_authorization_requirements() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_execute_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
