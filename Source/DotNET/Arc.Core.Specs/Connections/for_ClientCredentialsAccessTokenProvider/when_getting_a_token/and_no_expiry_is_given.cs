// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ClientCredentialsAccessTokenProvider.when_getting_a_token;

public class and_no_expiry_is_given : given.a_client_credentials_provider
{
    ConnectionResolverAccessToken _token;

    void Establish() => _answer = """{ "access_token": "the-token" }""";

    async Task Because() => _token = await _provider.GetToken(_options, CancellationToken.None);

    [Fact] void should_expire_immediately() => _token.ExpiresAt.ShouldEqual(_now);
}
