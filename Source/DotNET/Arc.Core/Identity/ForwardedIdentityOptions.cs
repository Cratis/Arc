// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Configures opt-in verification of signed forwarded identity headers.
/// </summary>
/// <remarks>Also requires <see cref="ArcOptions.TrustForwardedIdentityHeaders"/>. Invalid enabled configuration fails forwarded authentication.</remarks>
public class ForwardedIdentityOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether forwarded identity requires a valid v1 signature. Defaults to false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the integration audience, bound into every signature independently of the HTTP Host header.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accepted keys. Provision 1–8 distinct key identifiers through a secret provider.
    /// </summary>
    public IList<ForwardedIdentityKey> Keys { get; set; } = [];
}
