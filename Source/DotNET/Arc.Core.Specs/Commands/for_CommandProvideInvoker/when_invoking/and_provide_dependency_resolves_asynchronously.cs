// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_provide_dependency_resolves_asynchronously : given.a_command_provide_invoker
{
    class Command
    {
        public bool Ran { get; private set; }

        public string Provide(TheDependency dependency)
        {
            Ran = true;
            return dependency.GetType().Name;
        }
    }

    DelayedAsyncResolver _resolver;
    Command _command;
    ValueTask<IReadOnlyList<object>> _pending;
    bool _ranBeforeRelease;
    bool _completedBeforeRelease;
    IReadOnlyList<object> _result;

    void Establish()
    {
        _resolver = new(new TheDependency());
        _command = new();
        _serviceProvider = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .BuildServiceProvider();
    }

    async Task Because()
    {
        _pending = Invoke(_command);
        _completedBeforeRelease = _pending.IsCompleted;
        _ranBeforeRelease = _command.Ran;
        _resolver.Release();
        _result = await _pending;
    }

    [Fact] void should_yield_while_the_dependency_is_pending() => _completedBeforeRelease.ShouldBeFalse();
    [Fact] void should_not_run_provide_before_the_dependency_is_available() => _ranBeforeRelease.ShouldBeFalse();
    [Fact] void should_run_provide_with_the_resolved_dependency() => _result[0].ShouldEqual(nameof(TheDependency));
}
