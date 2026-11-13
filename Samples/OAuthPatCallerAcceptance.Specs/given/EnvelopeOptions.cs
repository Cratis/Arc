// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OAuthPatCallerAcceptance.Specs.given;

public sealed record EnvelopeOptions
{
    public string? AccessToken { get; init; }
    public string Kind { get; init; } = "oauth-grant";
    public string CredentialId { get; init; } = "oauth-credential";
    public string Audience { get; init; } = SampleSettings.Audience;
    public bool OmitSignature { get; init; }
    public bool CorruptSignature { get; init; }
    public bool UnknownKey { get; init; }
    public bool IncludeSessionCookie { get; init; }
    public bool CredentialInUri { get; init; }
    public bool RetiredCredentialInUri { get; init; }
    public bool UnsignedTenantOverride { get; init; }
    public bool DuplicateAuthorization { get; init; }
    public bool OmitBearer { get; init; }
    public bool LegacySignature { get; init; }
    public string? Nonce { get; init; }
    public IReadOnlyDictionary<string, string?> Headers { get; init; } = new Dictionary<string, string?>();

    public override string ToString() => "Synthetic envelope options [redacted]";
}
