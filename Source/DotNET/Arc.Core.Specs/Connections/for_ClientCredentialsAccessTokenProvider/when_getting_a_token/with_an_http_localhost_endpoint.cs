// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ClientCredentialsAccessTokenProvider.when_getting_a_token;

public class with_an_http_localhost_endpoint : given.a_client_credentials_provider
{
    ConnectionResolverAccessToken _token;

    void Establish() => _options.TokenEndpoint = new("http://localhost/token");

    async Task Because() => _token = await _provider.GetToken(_options, CancellationToken.None);

    [Fact] void should_post_to_the_token_endpoint() => _sentRequest!.RequestUri.ShouldEqual(_options.TokenEndpoint);
    [Fact] void should_send_the_client_secret() => _sentBody.ShouldContain("client_secret=the-secret");
    [Fact] void should_return_the_token() => _token.Token.ShouldEqual("the-token");
}
