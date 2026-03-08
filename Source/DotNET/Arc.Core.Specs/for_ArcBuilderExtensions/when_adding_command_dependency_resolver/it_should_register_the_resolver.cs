// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Arc.for_ArcBuilderExtensions.when_adding_command_dependency_resolver;

public class it_should_register_the_resolver : given.an_arc_builder
{
    void Because() => _builder.WithCommandDependencyResolver<TestCommandDependencyResolver>();

    [Fact] void should_register_resolver_service_type() =>
        _services.ShouldContain(_ => _.ServiceType == typeof(ICommandDependencyResolver) && _.ImplementationType == typeof(TestCommandDependencyResolver));

    [Fact] void should_register_as_singleton() =>
        _services.ShouldContain(_ => _.ServiceType == typeof(ICommandDependencyResolver) && _.Lifetime == ServiceLifetime.Singleton);
}

file class TestCommandDependencyResolver : ICommandDependencyResolver
{
    public bool CanResolve(Type type) => false;

    public Catch<object> Resolve(Type type, object command, CommandContextValues values, IServiceProvider serviceProvider) =>
        Catch<object>.Failed(new NotSupportedException());
}
