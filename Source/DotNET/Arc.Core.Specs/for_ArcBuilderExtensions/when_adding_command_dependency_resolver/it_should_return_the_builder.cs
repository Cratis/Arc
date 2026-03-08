// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Monads;

#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Arc.for_ArcBuilderExtensions.when_adding_command_dependency_resolver;

public class it_should_return_the_builder : given.an_arc_builder
{
    IArcBuilder _result;

    void Because() => _result = _builder.WithCommandDependencyResolver<TestResolver>();

    [Fact] void should_return_the_builder() => _result.ShouldEqual(_builder);
}

file class TestResolver : ICommandDependencyResolver
{
    public bool CanResolve(Type type) => false;

    public Catch<object> Resolve(Type type, object command, CommandContextValues values, IServiceProvider serviceProvider) =>
        Catch<object>.Failed(new NotSupportedException());
}
