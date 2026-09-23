// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents one authorization requirement declared on a type or method: the caller must be authenticated and,
/// when roles are given, hold at least one of them.
/// </summary>
/// <param name="AnyOfRoles">The roles the caller must hold at least one of, or empty when authentication alone suffices.</param>
/// <remarks>
/// Every requirement declared on a member must be satisfied, so two stacked authorization attributes require both,
/// which matches how ASP.NET Core combines its own <c>[Authorize]</c> attributes.
/// </remarks>
public record AuthorizationRequirement(IReadOnlyList<string> AnyOfRoles)
{
    /// <summary>
    /// Creates a requirement from a comma-separated roles value as carried by an authorization attribute.
    /// </summary>
    /// <param name="roles">The comma-separated roles, or null or empty when authentication alone suffices.</param>
    /// <returns>The <see cref="AuthorizationRequirement"/>.</returns>
    public static AuthorizationRequirement FromRoles(string? roles) =>
        new(string.IsNullOrWhiteSpace(roles)
            ? []
            : roles.Split(',').Select(role => role.Trim()).Where(role => role.Length > 0).ToArray());
}
