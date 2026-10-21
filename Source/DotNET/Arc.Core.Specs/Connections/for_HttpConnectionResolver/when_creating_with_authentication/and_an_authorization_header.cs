// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_creating_with_authentication;

public class and_an_authorization_header : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new HttpConnectionResolver(
        new()
        {
            Endpoint = new("https://platform.example/connections"),
            Headers = new Dictionary<string, string> { ["authorization"] = "Bearer static" },
            Authentication = new() { Type = HttpConnectionResolverAuthenticationType.ClientCredentials }
        },
        new HttpClient()));

    [Fact] void should_fail_with_a_conflict() => _error.ShouldBeOfExactType<HttpConnectionResolverAuthorizationHeaderConflict>();
}
