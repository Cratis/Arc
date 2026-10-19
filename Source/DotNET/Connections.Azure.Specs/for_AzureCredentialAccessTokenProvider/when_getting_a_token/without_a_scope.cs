// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.Azure.for_AzureCredentialAccessTokenProvider.when_getting_a_token;

public class without_a_scope : given.an_azure_credential_provider
{
    Exception _error;

    void Establish() => _options.Scope = null;

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetToken(_options, CancellationToken.None));

    [Fact] void should_fail_with_not_configured() => _error.ShouldBeOfExactType<HttpConnectionResolverAuthenticationNotConfigured>();
    [Fact] void should_not_ask_the_credential() => _requested.ShouldBeNull();
}
