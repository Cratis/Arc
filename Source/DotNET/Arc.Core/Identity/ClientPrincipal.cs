// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the structure of a Microsoft Client Principal.
/// </summary>
/// <remarks>
/// This is based on the definition found here: https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data.
/// </remarks>
public class ClientPrincipal
{
    /// <summary>
    /// Gets or sets the auth type - also referred to as the IDP type.
    /// </summary>
    public string IdentityProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user details.
    /// </summary>
    public string UserDetails { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user roles.
    /// </summary>
    public IEnumerable<string> UserRoles { get; set; } = [];

    /// <summary>
    /// Get or sets the claims.
    /// </summary>
    public IEnumerable<ClientPrincipalClaim> Claims { get; set; } = [];
}
