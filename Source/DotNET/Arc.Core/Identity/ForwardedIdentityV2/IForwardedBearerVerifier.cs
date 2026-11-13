// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Independently verifies a covered bearer credential.</summary>
internal interface IForwardedBearerVerifier
{
    /// <summary>Verifies through exactly the selected credential source.</summary>
    /// <param name="kind">The selected verifier.</param>
    /// <param name="authorizationValue">The exact authorization value; never log it.</param>
    /// <param name="context">The already verified signed credential context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Verified credential facts or refusal.</returns>
    Task<ForwardedBearerVerification> Verify(ForwardedBearerVerifierKind kind, string authorizationValue, ForwardedIdentityV2Context context, CancellationToken cancellationToken);
}
