// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

public class when_resolving_a_synchronous_parameter_asynchronously : Specification
{
    ServiceProvider _services;
    readonly TheDependency _dependency = new();
    object? _result;
    Resolver _resolver;
    ParameterInfo _parameter;

    void Establish()
    {
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
        _resolver = new(_dependency);
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .BuildServiceProvider();
    }

    async Task Because() => _result = await ParameterDependencyResolver.ResolveAsync(_services, _parameter, _ => new InvalidOperationException());
    void Destroy() => _services.Dispose();

    [Fact] void should_adapt_the_existing_synchronous_resolver() => _result.ShouldBeSame(_dependency);
    [Fact] void should_preserve_the_original_parameter_metadata() => _resolver.Parameter.ShouldBeSame(_parameter);

    sealed class Resolver(TheDependency value) : ICanResolveParameterDependency
    {
        public ParameterInfo? Parameter { get; private set; }
        public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
        {
            Parameter = parameter;
            dependency = value;
            return true;
        }
    }
}
