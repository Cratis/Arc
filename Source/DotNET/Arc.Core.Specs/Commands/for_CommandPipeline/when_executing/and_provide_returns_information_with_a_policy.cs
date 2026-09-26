// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_provide_returns_information_with_a_policy : given.a_command_with_validation_policy
{
    CommandResult _result;

    void Establish()
    {
        // Filters pass; this control result is emitted only by Provide().
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ => Task.FromResult(CommandResult.Success(_correlationId)));
        _commandHandlerArgumentResolver.Resolve(
            Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(_ => new ValueTask<CommandHandlerArgumentResolution>(new CommandHandlerArgumentResolution([], new CommandResult
            {
                CorrelationId = _correlationId,
                ValidationResults = [ValidationResult.Information("Provided failure")]
            })));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider, ValidationResultSeverity.Error);

    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
    [Fact] void should_preserve_information_from_provide() => _result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Information);
    [Fact] void should_preserve_the_message() => _result.ValidationResults.Single().Message.ShouldEqual("Provided failure");
    [Fact] void should_pass_the_effective_threshold_to_provide() => _commandHandlerArgumentResolver.Received(1).Resolve(
        Arg.Any<ICommandHandler>(),
        Arg.Is<CommandContext>(context => context.AllowedSeverity == ValidationResultSeverity.Unknown),
        Arg.Any<IServiceProvider>(),
        ValidationResultSeverity.Unknown);
}
