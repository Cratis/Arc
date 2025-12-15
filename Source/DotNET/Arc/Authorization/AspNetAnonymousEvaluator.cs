// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="IAnonymousEvaluator"/> that checks for the ASP.NET Core <see cref="AllowAnonymousAttribute"/>.
/// </summary>
public class AspNetAnonymousEvaluator : IAnonymousEvaluator
{
    /// <inheritdoc/>
    public bool? IsAnonymousAllowed(Type type)
    {
        var hasAllowAnonymous = type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0;
        var hasAuthorize = type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Length > 0;

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
        var hasAllowAnonymous = method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0;
        var hasAuthorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Length > 0;

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
