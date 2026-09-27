// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Certifies the execution identity of an asynchronous policy verdict until the protected member is invoked.
/// </summary>
/// <param name="Target">The authorized command type or query method.</param>
/// <param name="Principal">The execution identity captured before the policy verdict.</param>
/// <param name="Guest">Whether the verdict evaluated an unauthenticated caller.</param>
internal sealed record AuthorizedExecution(MemberInfo Target, PrincipalSnapshot Principal, bool Guest)
{
    /// <summary>
    /// Checks that the authorized identity is still the one about to execute the protected member.
    /// </summary>
    /// <param name="target">The member about to be invoked.</param>
    /// <param name="accessor">The current execution principal.</param>
    /// <returns>True when the policy verdict still certifies this execution.</returns>
    internal bool IsCurrent(MemberInfo target, ICurrentPrincipalAccessor accessor) =>
        Target.Equals(target) &&
        AuthorizationPrincipalIdentity.Same(Principal, accessor.Current) &&
        (!Guest || !AuthorizationEvaluator.HasAuthenticatedIdentity(accessor.Current));
}
