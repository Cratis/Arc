// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents a public key an on-behalf assertion may be signed with.
/// </summary>
/// <remarks>
/// The key must be an RSA public key of at least 2048 bits in PEM form (<c>-----BEGIN PUBLIC KEY-----</c> or
/// <c>-----BEGIN RSA PUBLIC KEY-----</c>). A private key is refused, so a signing secret never ends up in the
/// application's configuration.
/// </remarks>
public class OnBehalfOfSigningKey
{
    /// <summary>
    /// Gets or sets the key identifier, matched against the <c>kid</c> header of the assertion.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the RSA public key in PEM form.
    /// </summary>
    public string PublicKeyPem { get; set; } = string.Empty;
}
