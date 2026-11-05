// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>Configures client-certificate authentication of the immediate forwarding hop.</summary>
public sealed class ForwardedIdentityClientCertificateOptions
{
    /// <summary>Gets or sets whether the immediate TLS peer must present an admitted client certificate.</summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets the PEM bundle of trusted client-certificate authorities. System roots are never used.</summary>
    public string? TrustedCertificateAuthoritiesPath { get; set; }

    /// <summary>Gets or sets the exact, ordinal DNS SAN names admitted for the forwarding peer.</summary>
    public IList<string> AllowedDnsNames { get; set; } = [];

    /// <summary>Gets or sets how often the authority bundle is reloaded.</summary>
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromMinutes(1);
}
