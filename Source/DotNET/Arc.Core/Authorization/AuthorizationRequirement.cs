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
    /// Gets the named policy, if any.
    /// </summary>
    public string? Policy { get; init; }

    /// <summary>
    /// Gets the explicitly requested authentication schemes.
    /// </summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; init; } = [];

    /// <summary>
    /// Creates a requirement from a comma-separated roles value as carried by an authorization attribute.
    /// </summary>
    /// <param name="roles">The comma-separated roles, or null or empty when authentication alone suffices.</param>
    /// <returns>The <see cref="AuthorizationRequirement"/>.</returns>
    public static AuthorizationRequirement FromRoles(string? roles) =>
        new(string.IsNullOrWhiteSpace(roles)
            ? []
            : roles.Split(',').Select(role => role.Trim()).Where(role => role.Length > 0).ToArray());

    /// <summary>
    /// Creates a requirement from the settings of an authorization attribute.
    /// </summary>
    /// <param name="roles">Comma-separated roles.</param>
    /// <param name="policy">The named policy.</param>
    /// <param name="schemes">Comma-separated authentication schemes.</param>
    /// <returns>The complete requirement.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">A policy name is whitespace or a scheme name in the list is empty.</exception>
    public static AuthorizationRequirement FromAttribute(string? roles, string? policy, string? schemes)
    {
        if (policy is { Length: > 0 } && string.IsNullOrWhiteSpace(policy))
        {
            throw new InvalidAuthorizationConfiguration("The authorization policy name contains only whitespace.");
        }

        var selectedSchemes = string.IsNullOrEmpty(schemes)
            ? []
            : schemes.Split(',').Select(scheme => scheme.Trim()).ToArray();
        if (selectedSchemes.Any(string.IsNullOrEmpty))
        {
            throw new InvalidAuthorizationConfiguration("AuthenticationSchemes contains an empty scheme name.");
        }

        return FromRoles(roles) with { Policy = policy, AuthenticationSchemes = selectedSchemes };
    }
}
