// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace VerifiedCallerAcceptance.Specs.given;

public sealed record RequestOptions
{
    public string Scope { get; init; } = "content:write";
    public string Subject { get; init; } = VerifiedCallerAcceptanceSettings.Editor;
    public string Tenant { get; init; } = "tenant-a";
    public string? Nonce { get; init; }
    public string Audience { get; init; } = VerifiedCallerAcceptanceSettings.Audience;
    public string? KeyId { get; init; }
    public bool OmitSignature { get; init; }
}
