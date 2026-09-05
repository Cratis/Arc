// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a named authorization rule declared at the document level.
/// </summary>
/// <param name="Name">The name of the policy.</param>
/// <param name="Requirement">
/// What the policy requires of the caller, or <see langword="null"/> when it could not be recovered.
/// </param>
public record PolicyModel(string Name, PolicyRequirementModel? Requirement)
{
    /// <summary>
    /// Creates the policy a role implies.
    /// </summary>
    /// <param name="role">The name of the role.</param>
    /// <returns>The <see cref="PolicyModel"/>.</returns>
    public static PolicyModel ForRole(string role) => new(role, new RoleRequirement(role));
}
