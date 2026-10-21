// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections.for_ConnectionResolverFactory.when_creating;

public class with_client_credentials_authentication : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => new ConnectionResolverFactory(new ServiceCollection().BuildServiceProvider())
        .Create(new()
        {
            Identity = new()
            {
                Resolver = ConnectionResolverType.Http,
                Http = new()
                {
                    Endpoint = new("https://platform.example/connections"),
                    Authentication = new() { Type = HttpConnectionResolverAuthenticationType.ClientCredentials }
                }
            }
        }));

    [Fact] void should_use_the_built_in_provider() => _error.ShouldBeNull();
}
