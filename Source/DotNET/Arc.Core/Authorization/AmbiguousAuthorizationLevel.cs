// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The exception that is thrown when a member has both <see cref="AuthorizeAttribute"/>
/// and <see cref="AllowAnonymousAttribute"/> defined.
/// </summary>
/// <param name="member">The member with ambiguous authorization.</param>
public class AmbiguousAuthorizationLevel(MemberInfo member)
    : Exception($"Member '{member.DeclaringType?.FullName}.{member.Name}' has both [Authorize] and [AllowAnonymous] attributes defined, which is ambiguous.")
{
}
