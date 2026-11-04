// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_resolving_asynchronously;

public class and_an_async_resolver_completes_later : Specification
{
    DelayedAsyncResolver _resolver;
    IServiceProvider _services;
    ParameterInfo _parameter;
    TheDependency _dependency;
    ValueTask<object?> _pending;
    bool _completedBeforeRelease;
    object? _result;

    void Establish()
    {
        _dependency = new();
        _resolver = new(_dependency);
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver.Resolver]))
            .BuildServiceProvider();
    }

    async Task Because()
    {
        _pending = ParameterDependencyResolver.ResolveAsync(_services, _parameter, _ => new InvalidOperationException());
        _completedBeforeRelease = _pending.IsCompleted;
        _resolver.Release();
        _result = await _pending;
    }

    [Fact] void should_yield_instead_of_completing_synchronously() => _completedBeforeRelease.ShouldBeFalse();
    [Fact] void should_use_the_asynchronous_contract() => _resolver.AsynchronousCalls.ShouldEqual(1);
    [Fact] void should_not_call_the_synchronous_contract() => _resolver.SynchronousCalls.ShouldEqual(0);
    [Fact] void should_resolve_the_dependency() => _result.ShouldBeSame(_dependency);
}
