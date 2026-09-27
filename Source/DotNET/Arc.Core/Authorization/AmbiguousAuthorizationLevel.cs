// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The exception that is thrown when a member has conflicting authorization declarations.
/// </summary>
public class AmbiguousAuthorizationLevel : Exception
{
    /// <summary>
    /// The exception that is thrown when a member carries both authorization attributes.
    /// </summary>
    /// <param name="member">The member with ambiguous authorization.</param>
    public AmbiguousAuthorizationLevel(MemberInfo member)
        : base($"Member '{member.DeclaringType?.FullName}.{member.Name}' has both [Authorize] and [AllowAnonymous] attributes defined, which is ambiguous.")
    {
    }

    /// <summary>
    /// The exception that is thrown when evaluator declarations disagree about anonymous access.
    /// </summary>
    /// <param name="member">The member with ambiguous authorization.</param>
    /// <param name="anonymousEvaluator">The evaluator declaring anonymous access.</param>
    /// <param name="restrictedEvaluator">The evaluator declaring restricted access.</param>
    public AmbiguousAuthorizationLevel(MemberInfo member, Type anonymousEvaluator, Type restrictedEvaluator)
        : base($"Member '{member.DeclaringType?.FullName}.{member.Name}' has conflicting authorization declarations: '{anonymousEvaluator.FullName}' allows anonymous access and '{restrictedEvaluator.FullName}' requires authorization.")
    {
    }
}
