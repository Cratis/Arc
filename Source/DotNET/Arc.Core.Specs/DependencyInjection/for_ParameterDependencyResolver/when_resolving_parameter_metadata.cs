// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

public class when_resolving_parameter_metadata : Specification
{
    IServiceProvider _services;
    ParameterInfo[] _parameters;
    object?[] _result;
    TheDependency _first;
    TheDependency _second;

    void Establish()
    {
        _first = new();
        _second = new();
        _parameters = typeof(ConsumerWithParameters).GetMethod(nameof(ConsumerWithParameters.Handle))!.GetParameters();
        var resolver = Substitute.For<ICanResolveParameterDependency>();
        resolver.TryResolve(Arg.Any<IServiceProvider>(), _parameters[0], out Arg.Any<object?>()).Returns(call =>
        {
            call[2] = _first;
            return true;
        });
        resolver.TryResolve(Arg.Any<IServiceProvider>(), _parameters[1], out Arg.Any<object?>()).Returns(call =>
        {
            call[2] = _second;
            return true;
        });
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([resolver]))
            .BuildServiceProvider();
    }

    void Because() => _result = ParameterDependencyResolver.Resolve(_services, _parameters, _ => new Unresolved());

    [Fact] void should_resolve_the_first_parameter_independently() => _result[0].ShouldBeSame(_first);
    [Fact] void should_resolve_the_second_parameter_independently() => _result[1].ShouldBeSame(_second);

    class ConsumerWithParameters
    {
        public void Handle(TheDependency first, TheDependency second) { }
    }

    class Unresolved : Exception;
}
