// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OAuthPatCallerAcceptance;

/// <summary>Synthetic authoritative introspection data, independent of signed forwarding headers.</summary>
public sealed record TokenRecord
{
    /// <summary>Gets the token issuer.</summary>
    public string Issuer { get; init; } = SampleSettings.Issuer;

    /// <summary>Gets the account subject.</summary>
    public string Subject { get; init; } = SampleSettings.Subject;

    /// <summary>Gets the token identifier.</summary>
    public string CredentialId { get; init; } = "oauth-credential";

    /// <summary>Gets the intended resource.</summary>
    public string Resource { get; init; } = SampleSettings.Resource;

    /// <summary>Gets the intended audience.</summary>
    public string Audience { get; init; } = SampleSettings.Audience;

    /// <summary>Gets the environment.</summary>
    public string Environment { get; init; } = "acceptance";

    /// <summary>Gets the expiry.</summary>
    public DateTimeOffset Expires { get; init; } = DateTimeOffset.UtcNow.AddMinutes(5);

    /// <summary>Gets whether the token is revoked.</summary>
    public bool Revoked { get; init; }

    /// <summary>Gets whether the credential represents a machine, not a person grant.</summary>
    public bool ClientCredentials { get; init; }

    /// <summary>Gets independently granted scopes.</summary>
    public IReadOnlySet<string> Scopes { get; init; } = new HashSet<string> { "read" };

    /// <summary>Gets the token's client identifier.</summary>
    public string? ClientId { get; init; } = "sample-client";

    /// <summary>Gets the token's cnf evidence; null attests absence.</summary>
    public string? ConfirmationKeyThumbprint { get; init; }
}
