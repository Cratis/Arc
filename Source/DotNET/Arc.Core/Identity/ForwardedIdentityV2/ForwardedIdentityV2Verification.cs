// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Either a complete verification result or a fixed refusal without facts.</summary>
internal sealed class ForwardedIdentityV2Verification
{
    ForwardedIdentityV2Verification(ForwardedIdentityV2Context? context, ForwardedBearerVerification? bearer, ForwardedIdentityV2Rejection? rejection)
    {
        Context = context;
        Bearer = bearer;
        Rejection = rejection;
    }

    /// <summary>Gets the verified signed context, if accepted.</summary>
    internal ForwardedIdentityV2Context? Context { get; }

    /// <summary>Gets the independent bearer result, if applicable.</summary>
    internal ForwardedBearerVerification? Bearer { get; }

    /// <summary>Gets the fixed refusal reason.</summary>
    internal ForwardedIdentityV2Rejection? Rejection { get; }

    /// <summary>Gets the independent source of bearer facts.</summary>
    internal ForwardedIdentityV2CallerSource CallerSource => Bearer?.Kind switch
    {
        ForwardedBearerVerifierKind.Pat => ForwardedIdentityV2CallerSource.Pat,
        ForwardedBearerVerifierKind.OAuth => ForwardedIdentityV2CallerSource.OAuth,
        _ => ForwardedIdentityV2CallerSource.None
    };

    /// <summary>Creates an accepted result.</summary>
    /// <param name="context">The verified context.</param>
    /// <param name="bearer">The independently verified bearer.</param>
    /// <returns>The accepted result.</returns>
    internal static ForwardedIdentityV2Verification Accepted(ForwardedIdentityV2Context context, ForwardedBearerVerification? bearer) => new(context, bearer, null);

    /// <summary>Creates a refusal without context or bearer facts.</summary>
    /// <param name="reason">The fixed reason.</param>
    /// <returns>The refused result.</returns>
    internal static ForwardedIdentityV2Verification Rejected(ForwardedIdentityV2Rejection reason) => new(null, null, reason);
}
