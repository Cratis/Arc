// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_an_execution_scope_fails_to_complete_among_multiple : given.a_command_pipeline_and_a_handler_for_command
{
    ICommandExecutionScope _firstScope;
    ICommandExecutionScope _secondScope;
    CommandPipeline _pipeline;
    CommandResult _result;

    void Establish()
    {
        _firstScope = Substitute.For<ICommandExecutionScope>();
        _secondScope = Substitute.For<ICommandExecutionScope>();
        _secondScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Throws(new Exception("Second scope failed"));
        var activitySource = Substitute.For<IActivitySource<CommandPipeline>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activitySource.ActualSource.Returns(_activitySource);
        _pipeline = new(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder,
            _commandHandlerArgumentResolver,
            new KnownInstancesOf<ICommandExecutionScope>([_firstScope, _secondScope]),
            _serviceScopeFactory,
            activitySource);
    }

    async Task Because() => _result = await _pipeline.Execute(_command, _serviceProvider);

    [Fact] void should_still_complete_the_other_scope() => _firstScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
    [Fact] void should_fold_the_failure_into_the_result() => _result.HasExceptions.ShouldBeTrue();
}
