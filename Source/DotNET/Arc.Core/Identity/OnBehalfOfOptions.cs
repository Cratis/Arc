// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the options for accepting on-behalf forwards from Cratis AuthProxy.
/// </summary>
/// <remarks>
/// <para>
/// Bound from <c>Cratis:Arc:OnBehalfOf</c>. On-behalf forwarding is off by default: a request carrying
/// <see cref="OnBehalfOfHeaders.ActorHeader"/> or <see cref="OnBehalfOfHeaders.AssertionHeader"/> fails authentication
/// until <see cref="Enabled"/> is set. When enabled, every on-behalf forward must carry an RS256 assertion signed by one of
/// the <see cref="SigningKeys"/>, issued by <see cref="Issuer"/> for <see cref="Audience"/>, and bound to the exact identity
/// headers it travels with.
/// </para>
/// <para>
/// On-behalf forwarding also requires <see cref="ArcOptions.TrustForwardedIdentityHeaders"/>, because the represented user
/// arrives in the forwarded identity headers.
/// </para>
/// </remarks>
public class OnBehalfOfOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the host accepts on-behalf forwards. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the issuer the assertion must carry in its <c>iss</c> claim. Required when <see cref="Enabled"/> is set.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience the assertion must carry in its <c>aud</c> claim. Required when <see cref="Enabled"/> is set.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public keys an assertion may be signed with, selected by the <c>kid</c> of the assertion.
    /// Required when <see cref="Enabled"/> is set.
    /// </summary>
    public IList<OnBehalfOfSigningKey> SigningKeys { get; set; } = [];

    /// <summary>
    /// Gets or sets the clock skew allowed when checking the <c>nbf</c> and <c>exp</c> claims of an assertion.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);
}
