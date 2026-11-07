// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A test-only proxy base that keeps dynamic proxies out of Arc.Core's internal verification seam.
/// The inherited explicit implementation bridges to public, opaque proxy methods; the proxy assembly
/// never needs to implement the internal interface or reference its internal credential types.
/// </summary>
public class VerifiedCredentialSourceForSpecs : IVerifiedCredentialSource
{
    /// <summary>Gets the opaque credential shape configured by a spec.</summary>
    public virtual object? Shape => null;

    /// <inheritdoc/>
    PresentedCredentialShape IVerifiedCredentialSource.Shape => Shape is PresentedCredentialShape shape ? shape : default;

    /// <summary>Tests whether this source consumes the named header.</summary>
    /// <param name="headerName">The header name.</param>
    /// <returns>Whether the source consumes it.</returns>
    public virtual bool ReadsHeader(string headerName) => false;

    /// <inheritdoc/>
    bool IVerifiedCredentialSource.Consumes(string headerName) => ReadsHeader(headerName);

    /// <summary>Supplies the opaque verification task configured by a spec.</summary>
    /// <param name="presentation">The copied credential presentation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configured verification task.</returns>
    public virtual object? Verify(object presentation, CancellationToken cancellationToken) => null;

    /// <inheritdoc/>
    Task<CredentialVerification> IVerifiedCredentialSource.Verify(CredentialPresentation presentation, CancellationToken cancellationToken) =>
        Verify(presentation, cancellationToken) as Task<CredentialVerification> ?? Task.FromResult(CredentialVerification.NotApplicable());
}
