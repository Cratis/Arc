// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_resolving_asynchronously;

public class and_a_sync_resolver_precedes_an_async_one : Specification
{
    DelayedAsyncResolver _async;
    IServiceProvider _services;
    ParameterInfo _parameter;
    TheDependency _fromSync;
    object? _result;

    void Establish()
    {
        _fromSync = new();
        _async = new(new TheDependency());
        var sync = Substitute.For<ICanResolveParameterDependency>();
        sync.TryResolve(Arg.Any<IServiceProvider>(), Arg.Any<ParameterInfo>(), out Arg.Any<object?>()).Returns(call =>
        {
            call[2] = _fromSync;
            return true;
        });
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([sync, _async.Resolver]))
            .BuildServiceProvider();
    }

    async Task Because() => _result = await ParameterDependencyResolver.ResolveAsync(_services, _parameter, _ => new InvalidOperationException());

    [Fact] void should_use_the_first_owning_resolver_in_order() => _result.ShouldBeSame(_fromSync);
    [Fact] void should_not_invoke_later_resolvers() => _async.AsynchronousCalls.ShouldEqual(0);
}
