// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_provide_short_circuits : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish()
    {
        var shortCircuit = new CommandResult
        {
            CorrelationId = _correlationId,
            ValidationResults = [ValidationResult.Error("Not allowed")]
        };
        _commandHandlerArgumentResolver
            .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(_ => new ValueTask<CommandHandlerArgumentResolution>(new CommandHandlerArgumentResolution([], shortCircuit)));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_return_not_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_the_validation_result() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_not_call_command_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
