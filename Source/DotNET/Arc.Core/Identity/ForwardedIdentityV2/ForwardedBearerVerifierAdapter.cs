// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Dispatches covered bearers to existing sources without a second HTTP identity pipeline.</summary>
/// <param name="services">The current request scope.</param>
internal sealed class ForwardedBearerVerifierAdapter(IServiceProvider services) : IForwardedBearerVerifier
{
    /// <inheritdoc/>
    async Task<ForwardedBearerVerification> IForwardedBearerVerifier.Verify(ForwardedBearerVerifierKind kind, string authorizationValue, CancellationToken cancellationToken)
    {
        try
        {
            // Resolve lazily in the request scope: the envelope source itself depends on this adapter.
            var shape = kind == ForwardedBearerVerifierKind.Pat ? PresentedCredentialShape.PatBearer : PresentedCredentialShape.OAuthBearer;
            var sources = services.GetServices<IVerifiedCredentialSource>().Where(source => source.Shape == shape).ToArray();
            if (sources.Length != 1) return ForwardedBearerVerification.Refused();
            var result = await sources[0].Verify(new CredentialPresentation([authorizationValue], new Dictionary<string, IReadOnlyList<string>>(), shape: shape), cancellationToken);
            return result?.Facts is { AccountKey: { } account } facts && result.Refusal is null && result.Applicable ? ForwardedBearerVerification.Accepted(kind, account, facts) : ForwardedBearerVerification.Refused();
        }
        catch (Exception)
        {
            return ForwardedBearerVerification.Refused();
        }
    }
}
