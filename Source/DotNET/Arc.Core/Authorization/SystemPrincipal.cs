// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Builds the <see cref="ClaimsPrincipal"/> used to represent a trusted system actor.
/// </summary>
public static class SystemPrincipal
{
    /// <summary>
    /// The authentication type used for the system principal. A non-empty authentication type makes
    /// <see cref="ClaimsIdentity.IsAuthenticated"/> return <see langword="true"/>.
    /// </summary>
    public const string AuthenticationType = "System";

    /// <summary>
    /// The subject and name used to identify the system actor in audit trails.
    /// </summary>
    public const string Subject = "[System]";

    /// <summary>
    /// Creates an authenticated system <see cref="ClaimsPrincipal"/> carrying the given roles.
    /// </summary>
    /// <param name="roles">The roles the system actor holds.</param>
    /// <returns>An authenticated <see cref="ClaimsPrincipal"/> with a role claim for each role.</returns>
    public static ClaimsPrincipal WithRoles(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Subject),
            new("sub", Subject),
            new(ClaimTypes.Name, Subject)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, AuthenticationType);
        return new ClaimsPrincipal(identity);
    }
}
