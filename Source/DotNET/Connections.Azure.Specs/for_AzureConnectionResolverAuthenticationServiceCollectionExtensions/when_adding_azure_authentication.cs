// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections.Azure.for_AzureConnectionResolverAuthenticationServiceCollectionExtensions;

public class when_adding_azure_authentication : Specification
{
    IConnectionResolverAccessTokenProvider[] _providers;
    Exception? _error;

    void Because()
    {
        var services = new ServiceCollection()
            .AddAzureConnectionResolverAuthentication()
            .AddAzureConnectionResolverAuthentication()
            .BuildServiceProvider();
        _providers = [.. services.GetServices<IConnectionResolverAccessTokenProvider>()];
        _error = Catch.Exception(() => services.GetRequiredService<IConnectionResolverFactory>().Create(new()
        {
            Identity = new()
            {
                Resolver = ConnectionResolverType.Http,
                Http = new()
                {
                    Endpoint = new("https://platform.example/connections"),
                    Authentication = new() { Type = HttpConnectionResolverAuthenticationType.AzureCredential, Scope = "api://platform/.default" }
                }
            }
        }));
    }

    [Fact] void should_register_the_azure_provider_once() => _providers.OfType<AzureCredentialAccessTokenProvider>().Count().ShouldEqual(1);
    [Fact] void should_keep_the_client_credentials_provider() => _providers.OfType<ClientCredentialsAccessTokenProvider>().Count().ShouldEqual(1);
    [Fact] void should_let_the_http_resolver_use_azure_credentials() => _error.ShouldBeNull();
}
