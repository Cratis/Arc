// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal sealed class VectorBearerVerifier(VerifiedBearerResult? result) : IForwardedBearerVerifier
{
    internal int Calls { get; private set; }
    internal ForwardedBearerVerifierKind? LastKind { get; private set; }

    public Task<ForwardedBearerVerification> Verify(ForwardedBearerVerifierKind kind, string authorizationValue, ForwardedIdentityV2Context context, CancellationToken cancellationToken)
    {
        Calls++;
        LastKind = kind;
        if (result?.Accepted != true || result.Verifier != (kind == ForwardedBearerVerifierKind.Pat ? "pat-verifier" : "oauth-verifier")) return Task.FromResult(ForwardedBearerVerification.Refused());
        var account = new CallerAccountKey(result.AccountKey.Issuer, result.AccountKey.Subject);
        var facts = new VerifiedCredentialFacts(CallerPrincipalKind.Person, kind == ForwardedBearerVerifierKind.Pat ? CallerCredentialKind.PersonalAccessToken : CallerCredentialKind.OAuthGrant, new("verified-bearer-id"), new("bearer-resource", "bearer-audience", "bearer-env"))
        {
            AccountKey = account,
            Person = new("persisted-person"),
            Scopes = new HashSet<string> { "bearer-only" }
        };
        return Task.FromResult(ForwardedBearerVerification.Accepted(kind, account, facts));
    }
}
