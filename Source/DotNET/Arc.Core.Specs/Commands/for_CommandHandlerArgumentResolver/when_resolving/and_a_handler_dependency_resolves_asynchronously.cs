// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_handler_dependency_resolves_asynchronously : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(TheDependency dependency) { }
    }

    DelayedAsyncResolver _resolver;
    TheDependency _dependency;
    ValueTask<CommandHandlerArgumentResolution> _pending;
    bool _completedBeforeRelease;
    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        _dependency = new();
        _resolver = new(_dependency);
        HandleHasParameters<Handler>();
        _serviceProvider = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver.Resolver]))
            .BuildServiceProvider();
    }

    async Task Because()
    {
        _pending = Resolve();
        _completedBeforeRelease = _pending.IsCompleted;
        _resolver.Release();
        _result = await _pending;
    }

    [Fact] void should_yield_while_the_dependency_is_pending() => _completedBeforeRelease.ShouldBeFalse();
    [Fact] void should_bind_the_awaited_dependency() => _result.Arguments[0].ShouldBeSame(_dependency);
    [Fact] void should_not_use_the_synchronous_contract() => _resolver.SynchronousCalls.ShouldEqual(0);
}
