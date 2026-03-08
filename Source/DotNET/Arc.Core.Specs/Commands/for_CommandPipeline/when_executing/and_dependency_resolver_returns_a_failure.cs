// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_dependency_resolver_returns_a_failure : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    ICommandDependencyResolver _resolver;
    Exception _resolverException;

    void Establish()
    {
        _resolverException = new InvalidOperationException("Resolution failed");
        _resolver = Substitute.For<ICommandDependencyResolver>();
        _resolver.CanResolve(typeof(object)).Returns(true);
        _resolver.Resolve(typeof(object), Arg.Any<object>(), Arg.Any<CommandContextValues>(), Arg.Any<IServiceProvider>())
            .Returns(Catch<object>.Failed(_resolverException));

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
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_call_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
