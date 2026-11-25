// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_a_discovered_resolver_is_not_registered;

public class and_resolving_synchronously : Specification
{
    ServiceProvider _services;
    ParameterInfo _parameter;
    Exception? _failure;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanResolveParameterDependency>().Returns([typeof(UnregisteredResolver)]);
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
        _services = new ServiceCollection()
            .AddSingleton(types)
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(provider => new InstancesOf<ICanResolveParameterDependency>(provider.GetRequiredService<ITypes>(), provider))
            .BuildServiceProvider();
    }

    void Because() => _failure = Catch.Exception(() => ParameterDependencyResolver.Resolve(_services, _parameter, _ => new InvalidOperationException()));

    void Destroy() => _services.Dispose();

    [Fact] void should_fail_with_cannot_resolve_dependency() => _failure.ShouldBeOfExactType<CannotResolveDependency>();
    [Fact] void should_keep_the_inner_failure() => _failure!.InnerException.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_missing_resolver() => _failure!.Message.ShouldContain(nameof(UnregisteredResolver));
    [Fact] void should_name_the_parameter() => _failure!.Message.ShouldContain("parameter 'dependency'");
    [Fact] void should_name_the_member() => _failure!.Message.ShouldContain($"{typeof(Consumer).FullName}.{nameof(Consumer.Method)}");
}
