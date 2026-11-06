// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Framework-only verification seam. Implementations must authenticate transport credentials and bind all facts before returning Verified.
/// NotApplicable does not claim a credential. Refused is terminal, not permission to try a weaker credential source.
/// The v1 envelope, arbitrary ClaimsPrincipal, and an OAuth jti alone cannot supply the required credential or grant identity.
/// No default source ships: v1 envelopes and host bearer validation cannot guarantee credential identity and assurance.
/// A later source may supply them from verified claims and server-side mappings. Only Arc friend assemblies can add sources before lock.
/// </summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal interface IVerifiedCredentialSource
{
    /// <summary>Gets the one credential shape this source owns.</summary>
    PresentedCredentialShape Shape { get; }

    /// <summary>Header names this source reads beyond the presentation.</summary>
    /// <param name="headerName">The header name.</param>
    /// <returns>Whether the source consumes the header.</returns>
    bool Consumes(string headerName);

    /// <summary>Verifies a copied presentation without consulting untrusted identity claims.</summary>
    /// <param name="presentation">The copied credentials.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Not applicable, a terminal refusal, or verified facts.</returns>
    Task<CredentialVerification> Verify(CredentialPresentation presentation, CancellationToken cancellationToken);
}
