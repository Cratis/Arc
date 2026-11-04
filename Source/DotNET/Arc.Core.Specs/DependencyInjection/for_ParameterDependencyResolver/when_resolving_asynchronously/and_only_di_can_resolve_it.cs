// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_resolving_asynchronously;

public class and_only_di_can_resolve_it : Specification
{
    IServiceProvider _services;
    ParameterInfo _parameter;
    TheDependency _registered;
    object? _result;

    void Establish()
    {
        _registered = new();
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
        _services = new ServiceCollection().AddSingleton(_registered).BuildServiceProvider();
    }

    async Task Because() => _result = await ParameterDependencyResolver.ResolveAsync(_services, _parameter, _ => new InvalidOperationException());

    [Fact] void should_fall_back_to_the_container() => _result.ShouldBeSame(_registered);
}
