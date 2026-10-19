// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections.for_ClientCredentialsAccessTokenProvider.when_getting_a_token;

public class and_the_endpoint_fails : given.a_client_credentials_provider
{
    Exception _error;

    void Establish() => _status = HttpStatusCode.BadRequest;

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetToken(_options, CancellationToken.None));

    [Fact] void should_fail_with_token_request_failed() => _error.ShouldBeOfExactType<ConnectionResolverAccessTokenRequestFailed>();
    [Fact] void should_name_the_client() => _error.Message.ShouldContain("the-client");
    [Fact] void should_name_the_scope() => _error.Message.ShouldContain("api://platform/.default");
    [Fact] void should_not_reveal_the_secret() => _error.Message.ShouldNotContain("the-secret");
}
