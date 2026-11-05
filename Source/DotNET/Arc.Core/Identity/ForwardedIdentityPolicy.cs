// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Derives permitted forms from the validated configuration, never from request headers.
/// Profile safety is enforced once at startup, not silently clamped at runtime.
/// </summary>
/// <param name="UnsignedV1">Whether unsigned v1 headers are accepted.</param>
/// <param name="SignedV1">Whether signed v1 envelopes are accepted.</param>
/// <param name="V2">Whether the internal v2 source is enabled.</param>
internal readonly record struct ForwardedIdentityPolicy(bool UnsignedV1, bool SignedV1, bool V2)
{
    /// <summary>Derives all forwarding forms from the master switch and configured signers.</summary>
    /// <param name="options">The validated host configuration.</param>
    /// <returns>The permitted forms.</returns>
    internal static ForwardedIdentityPolicy Read(ArcOptions options) => new(
        options.TrustForwardedIdentityHeaders && !options.ForwardedIdentity.Enabled && !options.ForwardedIdentityV2,
        options.TrustForwardedIdentityHeaders && options.ForwardedIdentity.Enabled,
        options.TrustForwardedIdentityHeaders && options.ForwardedIdentityV2);
}
