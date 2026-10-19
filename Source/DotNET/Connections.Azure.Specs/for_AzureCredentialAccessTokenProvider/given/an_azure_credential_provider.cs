// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;

namespace Cratis.Arc.Connections.Azure.for_AzureCredentialAccessTokenProvider.given;

public class an_azure_credential_provider : Specification
{
    protected DateTimeOffset _expiresOn;
    protected TokenCredential _credential;
    protected TokenRequestContext? _requested;
    protected HttpConnectionResolverAuthenticationOptions _options;
    protected AzureCredentialAccessTokenProvider _provider;

    void Establish()
    {
        _expiresOn = new(2026, 10, 15, 13, 0, 0, TimeSpan.Zero);
        _credential = Substitute.For<TokenCredential>();
        _credential.GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _requested = call.Arg<TokenRequestContext>();
                return new ValueTask<AccessToken>(new AccessToken("the-entra-token", _expiresOn));
            });
        _options = new()
        {
            Type = HttpConnectionResolverAuthenticationType.AzureCredential,
            Scope = "api://platform/.default"
        };
        _provider = new(_credential);
    }
}
