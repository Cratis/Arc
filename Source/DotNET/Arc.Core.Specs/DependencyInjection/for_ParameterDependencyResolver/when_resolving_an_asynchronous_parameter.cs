// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

public class when_resolving_an_asynchronous_parameter : Specification
{
    ServiceProvider _services;
    AsyncResolver _resolver;
    object?[] _result;
    bool _yielded;

    void Establish()
    {
        _resolver = new();
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .BuildServiceProvider();
    }

    async Task Because()
    {
        var pending = ParameterDependencyResolver.ResolveAsync(_services, typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters(), _ => new InvalidOperationException());
        _yielded = !pending.IsCompleted;
        _resolver.Completion.SetResult(new TheDependency());
        _result = await pending;
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_yield_until_the_dependency_completes() => _yielded.ShouldBeTrue();
    [Fact] void should_resolve_the_dependency() => _result[0].ShouldBeOfExactType<TheDependency>();

    public sealed class AsyncResolver : ICanResolveParameterDependency
    {
        public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency) => throw new InvalidOperationException("The synchronous resolver must not be called.");
        public async ValueTask<(bool Resolved, object? Dependency)> TryResolveAsync(IServiceProvider serviceProvider, ParameterInfo parameter) => (true, await Completion.Task);
    }
}
