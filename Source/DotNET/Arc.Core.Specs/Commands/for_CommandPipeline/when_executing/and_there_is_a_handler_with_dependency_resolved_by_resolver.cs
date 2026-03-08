// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_there_is_a_handler_with_dependency_resolved_by_resolver : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    object _resolvedDependency;
    List<object> _capturedDependencies = [];
    ICommandDependencyResolver _resolver;

    void Establish()
    {
        _resolvedDependency = new object();
        _resolver = Substitute.For<ICommandDependencyResolver>();
        _resolver.CanResolve(typeof(object)).Returns(true);
        _resolver.Resolve(typeof(object), Arg.Any<object>(), Arg.Any<CommandContextValues>(), Arg.Any<IServiceProvider>())
            .Returns(Catch<object>.Success(_resolvedDependency));

        _commandHandler.Dependencies.Returns([typeof(object)]);
        _dependencyResolvers = [_resolver];

        _commandPipeline = new(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder,
            _dependencyResolvers);

        _commandHandler.When(x => x.Handle(Arg.Any<CommandContext>())).Do(x =>
            _capturedDependencies.AddRange(x.Arg<CommandContext>().Dependencies));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_use_resolved_dependency() => _capturedDependencies.ShouldContainOnly([_resolvedDependency]);
    [Fact] void should_not_use_service_provider() => _serviceProvider.DidNotReceive().GetService(typeof(object));
}
