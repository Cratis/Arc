// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace OAuthPatCallerAcceptance;

/// <summary>Demonstrates independent opaque-token introspection. Replace the local authority with authenticated issuer introspection in production.</summary>
/// <param name="store">The synthetic authoritative store.</param>
public sealed class OAuthVerifier(SampleIdentityStore store) : IOAuthBearerVerifier
{
    /// <inheritdoc/>
    public Task<OAuthBearerVerification> Verify(OAuthBearerPresentation presentation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = store.Introspect(presentation.AccessToken);
        return Task.FromResult(token is not null && IsActivePersonGrant(token) && MatchesTarget(token, presentation) ? OAuthBearerVerification.Verified(new()
        {
            Issuer = token.Issuer,
            Subject = token.Subject,
            CredentialId = token.CredentialId,
            Scopes = token.Scopes,
            ClientId = token.ClientId,
            ConfirmationKeyThumbprint = token.ConfirmationKeyThumbprint
        }) : OAuthBearerVerification.Refused());
    }

    static bool IsActivePersonGrant(TokenRecord token) => !token.Revoked && !token.ClientCredentials && token.Expires > DateTimeOffset.UtcNow && token.Issuer == SampleSettings.Issuer;

    static bool MatchesTarget(TokenRecord token, OAuthBearerPresentation expected) =>
        token.Resource == expected.Resource && token.Audience == expected.Audience && token.Environment == expected.Environment;
}
