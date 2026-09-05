// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// An execution scope is resolved from the scope the command runs in, not from the provider that constructed this
/// singleton, so a scope depending on a scoped service is created in the command's scope rather than in the root.
/// </summary>
public class and_the_command_carries_its_own_scope : given.a_command_pipeline_and_a_handler_for_command
{
    ICommandExecutionScope _scopeFromTheScope;
    ICommandExecutionScope _scopeFromTheRoot;
    CommandPipeline _pipeline;

    void Establish()
    {
        _scopeFromTheScope = Substitute.For<ICommandExecutionScope>();
        _scopeFromTheRoot = Substitute.For<ICommandExecutionScope>();

        _serviceProvider.GetService(typeof(IInstancesOf<ICommandExecutionScope>))
            .Returns(new KnownInstancesOf<ICommandExecutionScope>([_scopeFromTheScope]));

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
            new KnownInstancesOf<ICommandExecutionScope>([_scopeFromTheRoot]),
            _serviceScopeFactory,
            activitySource);
    }

    async Task Because() => await _pipeline.Execute(_command, _serviceProvider);

    [Fact] void should_begin_the_execution_scope_from_the_scope() => _scopeFromTheScope.Received(1).Begin(Arg.Any<CommandContext>());
    [Fact] void should_complete_the_execution_scope_from_the_scope() => _scopeFromTheScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
    [Fact] void should_not_begin_the_execution_scope_from_the_root() => _scopeFromTheRoot.DidNotReceive().Begin(Arg.Any<CommandContext>());
}
