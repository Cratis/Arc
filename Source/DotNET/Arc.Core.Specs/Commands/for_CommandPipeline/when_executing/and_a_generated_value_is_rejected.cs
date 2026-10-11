// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_generated_value_is_rejected : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish() => _commandHandlerArgumentResolver
        .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
        .Returns<ValueTask<CommandHandlerArgumentResolution>>(_ => throw new GeneratedValueRejected(_command.GetType(), "id", typeof(Guid), ["sensitive validator message"]));

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_be_a_server_error() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_not_be_request_validation() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_not_expose_validator_failures() => _result.ExceptionMessages.ShouldNotContain(message => message.Contains("sensitive", StringComparison.Ordinal));
    [Fact] void should_not_invoke_handle() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
