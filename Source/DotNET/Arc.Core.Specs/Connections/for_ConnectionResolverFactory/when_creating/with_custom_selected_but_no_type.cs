// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections.for_ConnectionResolverFactory.when_creating;

public class with_custom_selected_but_no_type : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ConnectionResolverFactory(new ServiceCollection().BuildServiceProvider())
        .Create(new() { Identity = new() { Resolver = ConnectionResolverType.Custom } }));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<CustomConnectionResolverNotConfigured>();
}
