// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Admits forwarding peers at each header reader, independently of middleware order.
/// </summary>
internal static class ForwardedIdentityHop
{
    /// <summary>Checks the immediate peer before any forwarded headers are parsed.</summary>
    /// <param name="request">The current request.</param>
    /// <param name="options">The host configuration.</param>
    /// <returns>The admission decision.</returns>
    internal static ForwardedIdentityHopAdmission Admit(IHttpRequestContext request, ArcOptions options)
    {
        if (!options.ForwardedIdentity.ClientCertificate.Required) return ForwardedIdentityHopAdmission.NotRequired;
        if (request is not ICanReadClientCertificate transport) return ForwardedIdentityHopAdmission.NoTransportCertificate;
        using var certificate = transport.TransportClientCertificate;
        if (certificate is null) return ForwardedIdentityHopAdmission.NoClientCertificate;
        var logger = request.RequestServices?.GetService<Microsoft.Extensions.Logging.ILogger<ForwardedIdentityClientCertificateTrustStore>>() ?? NullLogger<ForwardedIdentityClientCertificateTrustStore>.Instance;
        var trust = ForwardedIdentityClientCertificateTrust.For(options.ForwardedIdentity.ClientCertificate.TrustedCertificateAuthoritiesPath, TimeProvider.System);
        return trust.Admit(certificate, options.ForwardedIdentity.ClientCertificate, logger);
    }

    /// <summary>Checks the mandatory v2 transport admission.</summary>
    /// <param name="request">The current request.</param>
    /// <returns>Whether a configured transport peer is authenticated.</returns>
    internal static bool IsAdmitted(IHttpRequestContext request) =>
        request.RequestServices?.GetService<IOptions<ArcOptions>>() is { } options && Admit(request, options.Value) == ForwardedIdentityHopAdmission.Authenticated;
}
