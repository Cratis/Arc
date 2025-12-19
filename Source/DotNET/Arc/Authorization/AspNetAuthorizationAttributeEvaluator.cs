// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="IAuthorizationAttributeEvaluator"/> that checks for the ASP.NET Core <see cref="AuthorizeAttribute"/>.
/// </summary>
public class AspNetAuthorizationAttributeEvaluator : IAuthorizationAttributeEvaluator
{
    /// <inheritdoc/>
    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type)
    {
        var authorizeAttribute = type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        if (authorizeAttribute is not null)
        {
            return (true, authorizeAttribute.Roles);
        }

        return null;
    }

    /// <inheritdoc/>
    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method)
    {
        var authorizeAttribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        if (authorizeAttribute is not null)
        {
            return (true, authorizeAttribute.Roles);
        }

        return null;
    }
}
