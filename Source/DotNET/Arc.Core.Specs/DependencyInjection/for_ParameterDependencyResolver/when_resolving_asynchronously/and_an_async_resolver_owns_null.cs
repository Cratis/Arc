// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_resolving_asynchronously;

public class and_an_async_resolver_owns_null : Specification
{
    DelayedAsyncResolver _resolver;
    IServiceProvider _services;
    ParameterInfo _parameter;
    Exception _error;

    void Establish()
    {
        _resolver = new(null);
        _resolver.Release();
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
        _services = new ServiceCollection()
            .AddSingleton(new TheDependency())
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .BuildServiceProvider();
    }

    async Task Because() => _error = await Catch.Exception(async () => await ParameterDependencyResolver.ResolveAsync(_services, _parameter, _ => new MissingDependency()));

    [Fact] void should_treat_owned_null_as_unresolved_instead_of_falling_back_to_di() => _error.ShouldBeOfExactType<MissingDependency>();

    class MissingDependency : Exception;
}
