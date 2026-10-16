// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections.for_ConnectionResolverFactory.when_creating;

public class with_http_selected_but_no_endpoint : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ConnectionResolverFactory(new ServiceCollection().BuildServiceProvider())
        .Create(new() { Identity = new() { Resolver = ConnectionResolverType.Http } }));

    [Fact] void should_fail_when_created_rather_than_when_first_used() => _error.ShouldBeOfExactType<HttpConnectionResolverEndpointNotConfigured>();
}
