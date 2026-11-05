// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity;

/// <summary>Centralizes fail-closed admission of forwarded credentials on the immediate TLS hop.</summary>
internal static class ForwardedIdentityHopAdmission
{
    /// <summary>Reads host-validated evidence, failing closed when the adapter cannot attest it.</summary>
    /// <param name="request">The current request.</param>
    /// <returns>Whether the hop is admitted.</returns>
    internal static bool IsAdmitted(IHttpRequestContext request) =>
        request is ICanReadAuthenticatedForwardingHop { IsAuthenticatedForwardingHop: true };

    /// <summary>Applies the opt-in v1 hop requirement.</summary>
    /// <param name="options">The host options.</param>
    /// <param name="request">The current request.</param>
    /// <returns>Whether v1 processing may proceed.</returns>
    internal static bool Allows(ArcOptions options, IHttpRequestContext request) =>
        !options.ForwardedIdentity.ClientCertificate.Required || IsAdmitted(request);
}
