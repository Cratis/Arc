// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.for_CommandPipeline.given;
using Cratis.Arc.Validation;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_warning_filter_precedes_an_error_filter_without_policy : a_command_pipeline
{
    record UnattributedCommand;

    ICommandFilter _warning;
    ICommandFilter _error;
    ICommandHandler _handler;
    CommandResult _executionResult;
    CommandResult _validationResult;

    void Establish()
    {
        _handler = Substitute.For<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(Arg.Any<UnattributedCommand>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _warning = Substitute.For<ICommandFilter>();
        _error = Substitute.For<ICommandFilter>();
        var source = Substitute.For<IActivitySource<CommandFilters>>();
        source.ActualSource.Returns(new System.Diagnostics.ActivitySource("Cratis.Arc.Test.Filters"));
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>([_warning, _error]), source);
        var pipelineSource = Substitute.For<IActivitySource<CommandPipeline>>();
        pipelineSource.ActualSource.Returns(new System.Diagnostics.ActivitySource("Cratis.Arc.Test.Pipeline"));
        _commandPipeline = new CommandPipeline(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder,
            _commandHandlerArgumentResolver,
            new KnownInstancesOf<ICommandExecutionScope>([_executionScope]),
            _serviceScopeFactory,
            pipelineSource);
    }

    async Task Because()
    {
        _warning.OnExecution(Arg.Any<CommandContext>()).Returns(Validation(ValidationResultSeverity.Warning));
        _error.OnExecution(Arg.Any<CommandContext>()).Returns(Validation(ValidationResultSeverity.Error));
        _executionResult = await _commandPipeline.Execute(new UnattributedCommand(), _serviceProvider);
        _validationResult = await _commandPipeline.Validate(new UnattributedCommand(), _serviceProvider);
    }

    CommandResult Validation(ValidationResultSeverity severity) => new()
    {
        CorrelationId = _correlationId,
        ValidationResults = [new ValidationResult(severity, severity.ToString(), [], null!)]
    };

    [Fact] void should_reject_execution_when_an_error_follows_a_warning() => _executionResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_error_in_execution() => _executionResult.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_reject_validation_when_an_error_follows_a_warning() => _validationResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_error_in_validation() => _validationResult.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_run_both_filters_on_each_path() => _error.Received(2).OnExecution(Arg.Any<CommandContext>());
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
