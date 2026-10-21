// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ClientCredentialsAccessTokenProvider.when_getting_a_token;

public class with_a_localhost_lookalike : given.a_client_credentials_provider
{
    Exception _error;

    void Establish() => _options.TokenEndpoint = new("http://localhost.example/token");

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetToken(_options, CancellationToken.None));

    [Fact] void should_require_https() => _error.ShouldBeOfExactType<ConnectionResolverTokenEndpointMustUseHttps>();
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain(nameof(HttpConnectionResolverAuthenticationOptions.TokenEndpoint));
    [Fact] void should_explain_the_requirement() => _error.Message.ShouldContain("HTTPS");
    [Fact] void should_not_send_the_secret() => _sentRequest.ShouldBeNull();
    [Fact] void should_not_reveal_the_secret() => _error.Message.ShouldNotContain("the-secret");
}
