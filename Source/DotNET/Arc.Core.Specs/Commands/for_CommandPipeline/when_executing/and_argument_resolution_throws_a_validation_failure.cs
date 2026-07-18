// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_argument_resolution_throws_a_validation_failure : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish() =>
        _commandHandlerArgumentResolver
            .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns<ValueTask<CommandHandlerArgumentResolution>>(_ => throw new TheValidationFailure());

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception_detail() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_surface_the_validation_message() => _result.ValidationResults.ShouldContain(vr => vr.Message == "missing identifier");
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    class TheValidationFailure : Exception, IValidationFailure
    {
        public ValidationResult ValidationResult { get; } = ValidationResult.Error("missing identifier");
    }
}
