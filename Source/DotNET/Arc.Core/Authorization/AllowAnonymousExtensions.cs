// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Extension methods for checking if anonymous access is allowed.
/// </summary>
public static class AllowAnonymousExtensions
{
    /// <summary>
    /// Checks if anonymous access is allowed for the specified member.
    /// </summary>
    /// <param name="member">The member to check for <see cref="AllowAnonymousAttribute"/>.</param>
    /// <returns>True if anonymous access is allowed, false otherwise.</returns>
    /// <remarks>
    /// This method checks the member itself first, then falls back to checking the declaring type.
    /// If the member has an explicit [Authorize] attribute without [AllowAnonymous], it will not allow anonymous access.
    /// </remarks>
    /// <exception cref="AmbiguousAuthorizationLevel">The member has both [Authorize] and [AllowAnonymous] attributes.</exception>
    public static bool IsAnonymousAllowed(this MemberInfo member)
    {
        var hasAllowAnonymous = Attribute.IsDefined(member, typeof(AllowAnonymousAttribute), inherit: true);
        var hasAuthorize = Attribute.IsDefined(member, typeof(AuthorizeAttribute), inherit: true);

        if (hasAllowAnonymous && hasAuthorize)
        {
            throw new AmbiguousAuthorizationLevel(member);
        }

        if (hasAllowAnonymous)
        {
            return true;
        }

        if (hasAuthorize)
        {
            return false;
        }

        return member.DeclaringType?.IsAnonymousAllowed() ?? false;
    }
}
