// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity;

/// <summary>
/// Detects forwarding attempts without reading their values.
/// </summary>
internal static class ForwardedIdentityHeaders
{
    /// <summary>Checks only names, without parsing any untrusted values.</summary>
    /// <param name="request">The current request.</param>
    /// <returns>Whether a forwarding header is present.</returns>
    internal static bool ArePresent(IHttpRequestContext request) =>
        UntrustedForwardedIdentityHeaders.ArePresent(request.Headers.ContainsKey) || ForwardedActor.IsPresent(request.Headers.ContainsKey) || request.Headers.ContainsKey(ForwardedIdentityEnvelope.Header);
}
