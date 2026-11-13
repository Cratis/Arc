// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace OAuthPatCallerAcceptance;

/// <summary>Public configuration for an isolated acceptance host. Certificates and ports belong to the caller.</summary>
public sealed class SampleSettings
{
    /// <summary>Gets the fixed receiving resource.</summary>
    public static string Resource => "https://api.example.test/mcp";

    /// <summary>Gets the fixed audience.</summary>
    public static string Audience => "example-api";

    /// <summary>Gets the trusted account issuer.</summary>
    public static string Issuer => "https://identity.example.test";

    /// <summary>Gets the persisted sample account.</summary>
    public static string Subject => "8b167ad2-0f89-4c78-9d42-6d4580495d32";

    /// <summary>Gets the capability listener port.</summary>
    public required int CapabilityPort { get; init; }

    /// <summary>Gets the default listener port.</summary>
    public required int DefaultPort { get; init; }

    /// <summary>Gets the public verification key for signed envelopes.</summary>
    public required string PublicKeyPem { get; init; }

    /// <summary>Gets the TLS server certificate.</summary>
    public required X509Certificate2 ServerCertificate { get; init; }

    /// <summary>Gets the forwarding peer CA bundle path.</summary>
    public required string TrustedCertificateAuthoritiesPath { get; init; }

    /// <summary>Gets whether the additional OAuth opt-in is installed.</summary>
    public bool AllowOAuth { get; init; } = true;

    /// <summary>Gets whether the MCP endpoint, like the unmarked REST endpoint, admits only PATs.</summary>
    public bool PatOnly { get; init; }

    /// <summary>Gets whether to resolve the caller before the capability gate, exercising its cache.</summary>
    public bool ResolveBeforeGate { get; init; }
}
