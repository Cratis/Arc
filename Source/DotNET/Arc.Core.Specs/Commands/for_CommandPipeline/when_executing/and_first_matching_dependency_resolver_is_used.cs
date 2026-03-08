// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_first_matching_dependency_resolver_is_used : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    object _firstResolved;
    object _secondResolved;
    List<object> _capturedDependencies = [];
    ICommandDependencyResolver _firstResolver;
    ICommandDependencyResolver _secondResolver;

    void Establish()
    {
        _firstResolved = new object();
        _secondResolved = new object();

        _firstResolver = Substitute.For<ICommandDependencyResolver>();
        _firstResolver.CanResolve(typeof(object)).Returns(true);
        _firstResolver.Resolve(typeof(object), Arg.Any<object>(), Arg.Any<CommandContextValues>(), Arg.Any<IServiceProvider>())
            .Returns(Catch<object>.Success(_firstResolved));

        _secondResolver = Substitute.For<ICommandDependencyResolver>();
        _secondResolver.CanResolve(typeof(object)).Returns(true);
        _secondResolver.Resolve(typeof(object), Arg.Any<object>(), Arg.Any<CommandContextValues>(), Arg.Any<IServiceProvider>())
            .Returns(Catch<object>.Success(_secondResolved));

        _commandHandler.Dependencies.Returns([typeof(object)]);
        _dependencyResolvers = [_firstResolver, _secondResolver];

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

    [Fact] void should_use_dependency_from_first_resolver() => _capturedDependencies.ShouldContainOnly([_firstResolved]);
    [Fact] void should_not_call_second_resolver_resolve() =>
        _secondResolver.DidNotReceive().Resolve(Arg.Any<Type>(), Arg.Any<object>(), Arg.Any<CommandContextValues>(), Arg.Any<IServiceProvider>());
}
