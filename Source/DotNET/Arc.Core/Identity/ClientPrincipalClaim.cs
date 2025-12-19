// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents a claim used in a client principal.
/// </summary>
public class ClientPrincipalClaim
{
#pragma warning disable CA1707 // Identifiers should not contain underscores
    /// <summary>
    /// Gets or sets the type of claim.
    /// </summary>
    public string typ { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the claim.
    /// </summary>
    public string val { get; set; } = string.Empty;
#pragma warning restore CA1707
}
