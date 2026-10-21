// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_creating_with_authentication;

public class and_no_provider_can_provide : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new HttpConnectionResolver(
        new()
        {
            Endpoint = new("https://platform.example/connections"),
            Authentication = new() { Type = HttpConnectionResolverAuthenticationType.AzureCredential }
        },
        new HttpClient()));

    [Fact] void should_fail_with_not_supported() => _error.ShouldBeOfExactType<HttpConnectionResolverAuthenticationNotSupported>();
    [Fact] void should_name_the_package() => _error.Message.ShouldContain("Cratis.Arc.Connections.Azure");
    [Fact] void should_name_the_registration() => _error.Message.ShouldContain("AddAzureConnectionResolverAuthentication()");
}
