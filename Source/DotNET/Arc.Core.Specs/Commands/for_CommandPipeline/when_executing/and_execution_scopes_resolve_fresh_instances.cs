// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_execution_scopes_resolve_fresh_instances : given.a_command_pipeline_and_a_handler_for_command
{
    FreshScopes _scopes;
    CommandPipeline _pipeline;

    void Establish()
    {
        _scopes = new FreshScopes();
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
            _scopes,
            _serviceScopeFactory,
            activitySource);
    }

    async Task Because() => await _pipeline.Execute(_command, _serviceProvider);

    [Fact] void should_enumerate_the_scopes_once() => _scopes.Enumerations.ShouldEqual(1);
    [Fact] void should_begin_and_complete_the_same_instance() => _scopes.Created.Single().Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());

    class FreshScopes : IInstancesOf<ICommandExecutionScope>
    {
        public List<ICommandExecutionScope> Created { get; } = [];

        public int Enumerations { get; private set; }

        public IEnumerator<ICommandExecutionScope> GetEnumerator()
        {
            Enumerations++;
            var scope = Substitute.For<ICommandExecutionScope>();
            Created.Add(scope);
            yield return scope;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
