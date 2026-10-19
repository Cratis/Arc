// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.Azure.for_AzureCredentialAccessTokenProvider.when_getting_a_token;

public class with_a_scope : given.an_azure_credential_provider
{
    ConnectionResolverAccessToken _token;

    async Task Because() => _token = await _provider.GetToken(_options, CancellationToken.None);

    [Fact] void should_request_the_scope() => _requested!.Value.Scopes.ShouldContainOnly("api://platform/.default");
    [Fact] void should_return_the_token() => _token.Token.ShouldEqual("the-entra-token");
    [Fact] void should_expire_when_the_credential_says() => _token.ExpiresAt.ShouldEqual(_expiresOn);
}
