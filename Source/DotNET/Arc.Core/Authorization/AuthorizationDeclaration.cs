// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// The effective authorization requirements on one member.
/// </summary>
/// <param name="AllowsAnonymous">Whether access is anonymous.</param>
/// <param name="IsExplicit">Whether an attribute declares authorization on this member.</param>
/// <param name="Requirements">All requirements, combined with AND.</param>
public record AuthorizationDeclaration(bool AllowsAnonymous, bool IsExplicit, IReadOnlyList<AuthorizationRequirement> Requirements)
{
    /// <summary>
    /// Gets whether a policy or explicit scheme needs asynchronous evaluation.
    /// </summary>
    public bool RequiresAsynchronousEvaluation => Requirements.Any(requirement =>
        !string.IsNullOrWhiteSpace(requirement.Policy) || requirement.AuthenticationSchemes.Count > 0);
}
