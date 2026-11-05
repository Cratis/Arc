// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Adapts host options to the process-wide private trust bundles.
/// </summary>
/// <param name="options">The host options.</param>
/// <param name="logger">The diagnostics logger.</param>
/// <param name="timeProvider">The reload clock.</param>
internal sealed class ForwardedIdentityClientCertificateTrustStore(IOptionsMonitor<ArcOptions> options, ILogger<ForwardedIdentityClientCertificateTrustStore> logger, TimeProvider? timeProvider = null) : IDisposable
{
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    /// <inheritdoc/>
    public void Dispose()
    {
        // Authority bundles are process-wide and shared by hosts using the same path.
    }

    /// <summary>Fails startup when required trust material is unavailable.</summary>
    /// <exception cref="OptionsValidationException">Required policy or initial trust material is invalid.</exception>
    internal void ValidateConfiguration()
    {
        var configured = options.CurrentValue.ForwardedIdentity.ClientCertificate;
        if (!configured.Required) return;
        if (!ForwardedIdentityClientCertificateTrust.IsConfigured(configured))
        {
            throw new OptionsValidationException(nameof(ArcOptions), typeof(ArcOptions), ["Forwarded identity client-certificate admission requires a CA bundle, DNS SAN allowlist, and reload interval of at least 5 seconds."]);
        }
        if (!Trust(configured).Load(configured.ReloadInterval, logger))
        {
            throw new OptionsValidationException(nameof(ArcOptions), typeof(ArcOptions), ["Forwarded identity client-certificate trust bundle is unavailable or invalid."]);
        }
    }

    /// <summary>Checks a transport certificate against the configured policy.</summary>
    /// <param name="certificate">The transport certificate.</param>
    /// <returns>Whether the certificate is admitted.</returns>
    internal bool IsTrusted(X509Certificate2 certificate)
    {
        var configured = options.CurrentValue.ForwardedIdentity.ClientCertificate;
        return configured.Required && Trust(configured).Admit(certificate, configured, logger) == ForwardedIdentityHopAdmission.Authenticated;
    }

    ForwardedIdentityClientCertificateTrust Trust(ForwardedIdentityClientCertificateOptions configured) =>
        ForwardedIdentityClientCertificateTrust.For(configured.TrustedCertificateAuthoritiesPath, _clock);
}
