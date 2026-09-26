// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.for_CommandPipeline.given;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_non_blocking_filter_precedes_a_blocking_filter : a_command_pipeline
{
    [BlockOnValidationSeverity(ValidationResultSeverity.Warning)]
    record PolicyCommand;

    ICommandFilter _first;
    ICommandFilter _second;
    ICommandHandler _handler;
    CommandResult _firstResult;
    CommandResult _reversedResult;

    void Establish()
    {
        var command = new PolicyCommand();
        _handler = Substitute.For<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(Arg.Any<PolicyCommand>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _first = Substitute.For<ICommandFilter>();
        _second = Substitute.For<ICommandFilter>();
        var source = Substitute.For<IActivitySource<CommandFilters>>();
        source.ActualSource.Returns(new System.Diagnostics.ActivitySource("Cratis.Arc.Test.Filters"));
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>([_first, _second]), source);
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
        _first.OnExecution(Arg.Any<CommandContext>()).Returns(Validation(ValidationResultSeverity.Information));
        _second.OnExecution(Arg.Any<CommandContext>()).Returns(Validation(ValidationResultSeverity.Error));
        _firstResult = await _commandPipeline.Execute(new PolicyCommand(), _serviceProvider);

        _first.OnExecution(Arg.Any<CommandContext>()).Returns(Validation(ValidationResultSeverity.Error));
        _second.OnExecution(Arg.Any<CommandContext>()).Returns(Validation(ValidationResultSeverity.Information));
        _reversedResult = await _commandPipeline.Execute(new PolicyCommand(), _serviceProvider);
    }

    CommandResult Validation(ValidationResultSeverity severity) => new()
    {
        CorrelationId = _correlationId,
        ValidationResults = [new ValidationResult(severity, severity.ToString(), [], null!)]
    };

    [Fact] void should_reject_when_information_precedes_error() => _firstResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_error_when_information_precedes_it() => _firstResult.ValidationResults.ShouldContain(_ => _.Severity == ValidationResultSeverity.Error);
    [Fact] void should_reject_when_error_precedes_information() => _reversedResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_run_later_filters_after_error() => _second.Received(1).OnExecution(Arg.Any<CommandContext>());
    [Fact] void should_never_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
