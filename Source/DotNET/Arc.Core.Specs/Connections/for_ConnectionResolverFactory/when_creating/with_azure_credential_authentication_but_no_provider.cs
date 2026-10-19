// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections.for_ConnectionResolverFactory.when_creating;

public class with_azure_credential_authentication_but_no_provider : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ConnectionResolverFactory(new ServiceCollection().AddConnectionResolution().BuildServiceProvider())
        .Create(new()
        {
            Identity = new()
            {
                Resolver = ConnectionResolverType.Http,
                Http = new()
                {
                    Endpoint = new("https://platform.example/connections"),
                    Authentication = new() { Type = HttpConnectionResolverAuthenticationType.AzureCredential }
                }
            }
        }));

    [Fact] void should_fail_when_created_rather_than_when_first_used() => _error.ShouldBeOfExactType<HttpConnectionResolverAuthenticationNotSupported>();
}
