// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Independently verifies an OAuth access token covered by an admitted signed v2 proof.</summary>
/// <remarks>The application owns signature or introspection validation, issuer, audience, expiry and revocation checks. Never log the presentation or token.</remarks>
public interface IOAuthBearerVerifier
{
    /// <summary>Verifies the exact token against the receiving target and signed identity.</summary>
    /// <param name="presentation">The token and independently verified signed-proof expectations.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Complete verified token evidence, or a terminal refusal.</returns>
    Task<OAuthBearerVerification> Verify(OAuthBearerPresentation presentation, CancellationToken cancellationToken);
}
