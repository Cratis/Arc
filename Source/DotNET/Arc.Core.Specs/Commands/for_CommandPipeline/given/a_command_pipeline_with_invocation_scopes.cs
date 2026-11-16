// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_command_pipeline_with_invocation_scopes : a_command_pipeline_and_a_handler_for_command
{
    protected ICommandInvocationExecutionScope _first;
    protected ICommandInvocationExecutionScope _second;
    protected ICommandInvocationExecutionScope _third;
    protected CommandPipeline _pipeline;

    void Establish()
    {
        _first = Substitute.For<ICommandInvocationExecutionScope>();
        _second = Substitute.For<ICommandInvocationExecutionScope>();
        _third = Substitute.For<ICommandInvocationExecutionScope>();
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
            new KnownInstancesOf<ICommandExecutionScope>([_first, _second, _third]),
            _serviceScopeFactory,
            activitySource);
    }
}
