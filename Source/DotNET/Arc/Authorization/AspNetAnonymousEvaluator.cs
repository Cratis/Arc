// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="IAnonymousEvaluator"/> that checks for ASP.NET Core's
/// <see cref="Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"/>.
/// </summary>
/// <remarks>
/// The ASP.NET Core attribute types are named in full on purpose. This file lives in the namespace that declares
/// Arc's own attributes of the same names, and a type in the enclosing namespace takes precedence over one brought in
/// by a <see langword="using"/> directive - so an unqualified name here silently means Arc's attribute, not ASP.NET Core's.
/// </remarks>
public class AspNetAnonymousEvaluator : IAnonymousEvaluator
{
    /// <inheritdoc/>
    /// <exception cref="AmbiguousAuthorizationLevel">Thrown when the type carries both ASP.NET Core attributes.</exception>
    public bool? IsAnonymousAllowed(Type type) => Evaluate(type);

    /// <inheritdoc/>
    /// <exception cref="AmbiguousAuthorizationLevel">Thrown when the method carries both ASP.NET Core attributes.</exception>
    public bool? IsAnonymousAllowed(MethodInfo method) => Evaluate(method);

    static bool? Evaluate(MemberInfo member)
    {
        var hasAllowAnonymous = Attribute.IsDefined(member, typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), inherit: true);
        var hasAuthorize = Attribute.IsDefined(member, typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true);

        if (hasAllowAnonymous && hasAuthorize)
        {
            throw new AmbiguousAuthorizationLevel(member);
        }

        if (hasAllowAnonymous)
        {
            return true;
        }

        return hasAuthorize ? false : null;
    }
}
