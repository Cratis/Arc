// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ClientCredentialsAccessTokenProvider.when_getting_a_token;

public class without_a_token_endpoint : given.a_client_credentials_provider
{
    Exception _error;

    void Establish() => _options.TokenEndpoint = null;

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetToken(_options, CancellationToken.None));

    [Fact] void should_fail_with_not_configured() => _error.ShouldBeOfExactType<HttpConnectionResolverAuthenticationNotConfigured>();
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain(nameof(HttpConnectionResolverAuthenticationOptions.TokenEndpoint));
    [Fact] void should_not_call_anything() => _sentRequest.ShouldBeNull();
}
