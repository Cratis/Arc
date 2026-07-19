// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_multiple_execution_scopes_participate : given.a_command_pipeline_and_a_handler_for_command
{
    ICommandExecutionScope _firstScope;
    ICommandExecutionScope _secondScope;
    CommandPipeline _pipeline;

    void Establish()
    {
        _firstScope = Substitute.For<ICommandExecutionScope>();
        _secondScope = Substitute.For<ICommandExecutionScope>();
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

    async Task Because() => await _pipeline.Execute(_command, _serviceProvider);

    [Fact] void should_begin_in_order_and_complete_in_reverse() => Received.InOrder(() =>
    {
        _firstScope.Begin(Arg.Any<CommandContext>());
        _secondScope.Begin(Arg.Any<CommandContext>());
        _secondScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
        _firstScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
    });
}
