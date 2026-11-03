// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents a provisioned HMAC-SHA256 integration key. Never reuse public test vectors in production.
/// </summary>
public class ForwardedIdentityKey
{
    /// <summary>
    /// Gets or sets the case-sensitive key ID: 1–64 ASCII letters, digits, underscores, or hyphens.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets exactly 32 random bytes encoded as standard base64. Supply this through a secret provider.
    /// </summary>
    public string SecretBase64 { get; set; } = string.Empty;
}
