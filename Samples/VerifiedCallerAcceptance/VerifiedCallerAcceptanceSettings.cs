// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace VerifiedCallerAcceptance;

/// <summary>
/// The explicit receiving resource and public verification material used by the sample.
/// </summary>
/// <param name="CapabilityPort">The capability listener port.</param>
/// <param name="DefaultPort">The default listener port.</param>
/// <param name="PublicKeyPem">Public-only P-256 PEM verification material.</param>
/// <param name="ServerCertificate">The capability listener's TLS certificate.</param>
/// <param name="TrustedCertificateAuthoritiesPath">The forwarding peer's trusted CA bundle.</param>
/// <param name="AllowedPeerDnsName">The allowed forwarding peer's DNS identity.</param>
/// <param name="KeyId">The exact key identifier.</param>
public sealed record VerifiedCallerAcceptanceSettings(int CapabilityPort, int DefaultPort, string PublicKeyPem, X509Certificate2 ServerCertificate, string TrustedCertificateAuthoritiesPath, string AllowedPeerDnsName, string KeyId = "sample")
{
    /// <summary>Gets the receiving resource.</summary>
    public static string Resource => "https://api.example.test";

    /// <summary>Gets the receiving audience.</summary>
    public static string Audience => "example-api";

    /// <summary>Gets the trusted realm issuer.</summary>
    public static string RealmIssuer => "https://identity.example.test";

    /// <summary>Gets the persisted person with editor membership.</summary>
    public static string Editor => "8b167ad2-0f89-4c78-9d42-6d4580495d32";

    /// <summary>Gets a persisted person without the editor role.</summary>
    public static string Reader => "25004c78-cb49-4464-8949-6301fa4ba175";

    /// <summary>Gets the tenant header included in signature coverage and tenant resolution.</summary>
    public static string TenantHeader => "x-example-tenant";
}
