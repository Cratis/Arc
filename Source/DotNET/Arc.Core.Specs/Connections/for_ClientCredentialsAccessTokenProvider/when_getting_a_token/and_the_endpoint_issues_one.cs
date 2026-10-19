// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ClientCredentialsAccessTokenProvider.when_getting_a_token;

public class and_the_endpoint_issues_one : given.a_client_credentials_provider
{
    ConnectionResolverAccessToken _token;

    async Task Because() => _token = await _provider.GetToken(_options, CancellationToken.None);

    [Fact] void should_post_to_the_token_endpoint() => _sentRequest!.RequestUri.ShouldEqual(_options.TokenEndpoint);
    [Fact] void should_post() => _sentRequest!.Method.ShouldEqual(HttpMethod.Post);
    [Fact] void should_post_a_form() => _sentRequest!.Content!.Headers.ContentType!.MediaType.ShouldEqual("application/x-www-form-urlencoded");
    [Fact] void should_use_the_client_credentials_grant() => _sentBody.ShouldContain("grant_type=client_credentials");
    [Fact] void should_send_the_client_id() => _sentBody.ShouldContain("client_id=the-client");
    [Fact] void should_send_the_client_secret() => _sentBody.ShouldContain("client_secret=the-secret");
    [Fact] void should_send_the_scope() => _sentBody.ShouldContain("scope=api%3A%2F%2Fplatform%2F.default");
    [Fact] void should_return_the_token() => _token.Token.ShouldEqual("the-token");
    [Fact] void should_expire_when_the_endpoint_says() => _token.ExpiresAt.ShouldEqual(_now.AddSeconds(3599));
    [Fact] void should_not_reveal_the_token_when_printed() => _token.ToString().ShouldNotContain("the-token");
}
