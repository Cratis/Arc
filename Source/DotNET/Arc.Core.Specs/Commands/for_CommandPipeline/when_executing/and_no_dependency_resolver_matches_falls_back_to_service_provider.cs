// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_no_dependency_resolver_matches_falls_back_to_service_provider : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    object _serviceProviderDependency;
    List<object> _capturedDependencies = [];
    ICommandDependencyResolver _resolver;

    void Establish()
    {
        _serviceProviderDependency = new object();

        _resolver = Substitute.For<ICommandDependencyResolver>();
        _resolver.CanResolve(typeof(object)).Returns(false);

        _commandHandler.Dependencies.Returns([typeof(object)]);
        _serviceProvider.GetService(typeof(object)).Returns(_serviceProviderDependency);
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

    [Fact] void should_use_dependency_from_service_provider() => _capturedDependencies.ShouldContainOnly([_serviceProviderDependency]);
    [Fact] void should_not_call_resolver_resolve() =>
        _resolver.DidNotReceive().Resolve(Arg.Any<Type>(), Arg.Any<object>(), Arg.Any<CommandContextValues>(), Arg.Any<IServiceProvider>());
}
