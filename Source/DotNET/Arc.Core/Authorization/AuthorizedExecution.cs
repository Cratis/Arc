// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Certifies the execution identity of an asynchronous policy verdict until the protected member is invoked.
/// </summary>
/// <param name="Target">The authorized command type or query method.</param>
/// <param name="Principal">The execution identity captured before the policy verdict.</param>
/// <param name="Declaration">The effective requirements checked by the verdict.</param>
/// <param name="Guest">Whether the verdict evaluated an unauthenticated caller.</param>
internal sealed record AuthorizedExecution(MemberInfo Target, PrincipalSnapshot Principal, AuthorizationDeclaration Declaration, bool Guest)
{
    readonly AuthorizationDeclaration _evaluatedDeclaration = Declaration with
    {
        Requirements = Declaration.Requirements.Select(requirement => requirement with
        {
            AnyOfRoles = requirement.AnyOfRoles.ToArray(),
            AuthenticationSchemes = requirement.AuthenticationSchemes.ToArray()
        }).ToArray()
    };

    /// <summary>
    /// Checks that the authorized identity is still the one about to execute the protected member.
    /// </summary>
    /// <param name="target">The member about to be invoked.</param>
    /// <param name="accessor">The current execution principal.</param>
    /// <param name="declaration">The current effective requirements for the target.</param>
    /// <returns>True when the policy verdict still certifies this execution.</returns>
    internal bool IsCurrent(MemberInfo target, ICurrentPrincipalAccessor accessor, AuthorizationDeclaration declaration) =>
        Target.Equals(target) &&
        AuthorizationEvaluator.SameDeclaration(_evaluatedDeclaration, declaration) &&
        AuthorizationPrincipalIdentity.Same(Principal, accessor.Current) &&
        (!Guest || !AuthorizationEvaluator.HasAuthenticatedIdentity(accessor.Current));
}
