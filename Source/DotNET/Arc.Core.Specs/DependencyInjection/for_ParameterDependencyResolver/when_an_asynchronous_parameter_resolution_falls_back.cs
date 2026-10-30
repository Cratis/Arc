// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

public class when_an_asynchronous_parameter_resolution_falls_back : Specification
{
    ServiceProvider _services;
    TheDependency _dependency;
    object? _result;

    void Establish()
    {
        _dependency = new();
        _services = new ServiceCollection()
            .AddSingleton(_dependency)
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([new DecliningResolver()]))
            .BuildServiceProvider();
    }

    async Task Because() => _result = await ParameterDependencyResolver.ResolveAsync(_services, typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0], _ => new InvalidOperationException());
    void Destroy() => _services.Dispose();

    [Fact] void should_use_the_service_provider_after_a_synchronous_resolver_declines() => _result.ShouldBeSame(_dependency);

    sealed class DecliningResolver : ICanResolveParameterDependency
    {
        public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
        {
            dependency = null;
            return false;
        }
    }
}
