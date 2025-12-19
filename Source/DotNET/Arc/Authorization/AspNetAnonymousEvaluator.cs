// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="IAnonymousEvaluator"/> that checks for the ASP.NET Core <see cref="AllowAnonymousAttribute"/>.
/// </summary>
public class AspNetAnonymousEvaluator : IAnonymousEvaluator
{
    /// <inheritdoc/>
    public bool? IsAnonymousAllowed(Type type)
    {
        var hasAllowAnonymous = Attribute.IsDefined(type, typeof(AllowAnonymousAttribute), inherit: true);
        var hasAuthorize = Attribute.IsDefined(type, typeof(AuthorizeAttribute), inherit: true);

        if (hasAllowAnonymous)
        {
            return true;
        }

        if (hasAuthorize)
        {
            return false;
        }

        return null;
    }

    /// <inheritdoc/>
    public bool? IsAnonymousAllowed(MethodInfo method)
    {
        var hasAllowAnonymous = Attribute.IsDefined(method, typeof(AllowAnonymousAttribute), inherit: true);
        var hasAuthorize = Attribute.IsDefined(method, typeof(AuthorizeAttribute), inherit: true);

        if (hasAllowAnonymous)
        {
            return true;
        }

        if (hasAuthorize)
        {
            return false;
        }

        return null;
    }
}
