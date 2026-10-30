// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

public class when_an_asynchronous_resolver_returns_an_owned_null : Specification
{
    ServiceProvider _services;
    object? _result;
    TheDependency _fallback;

    void Establish()
    {
        _fallback = new();
        var resolver = new when_resolving_an_asynchronous_parameter.AsyncResolver();
        resolver.Completion.SetResult(null);
        _services = new ServiceCollection()
            .AddSingleton(_fallback)
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([resolver]))
            .BuildServiceProvider();
    }

    async Task Because() => _result = await ParameterDependencyResolver.ResolveAsync(_services, typeof(Consumer).GetMethod(nameof(Consumer.Handle))!.GetParameters()[0], _ => new InvalidOperationException());
    void Destroy() => _services.Dispose();

    [Fact] void should_not_fall_back_to_the_registered_dependency() => _result.ShouldBeNull();

    class Consumer
    {
        public void Handle(TheDependency? dependency) { }
    }
}
