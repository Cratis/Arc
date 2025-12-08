// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Helper class for performing authorization checks.
/// </summary>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> to access the current HTTP context.</param>
public class AuthorizationEvaluator(IHttpContextAccessor httpContextAccessor) : IAuthorizationEvaluator
{
    /// <inheritdoc/>
    public bool IsAuthorized(Type type)
    {
        if (type.IsAnonymousAllowed())
        {
            return true;
        }

        var authorizeAttribute = type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        return IsAuthorizedWithAttribute(authorizeAttribute);
    }

    /// <inheritdoc/>
    public bool IsAuthorized(MethodInfo method)
    {
        if (method.IsAnonymousAllowed())
        {
            return true;
        }

        var methodAuthorizeAttribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        if (methodAuthorizeAttribute is not null)
        {
            return IsAuthorizedWithAttribute(methodAuthorizeAttribute);
        }

        var declaringType = method.DeclaringType;
        if (declaringType is not null)
        {
            return IsAuthorized(declaringType);
        }

        return true;
    }

    bool IsAuthorizedWithAttribute(AuthorizeAttribute? authorizeAttribute)
    {
        if (authorizeAttribute is null)
        {
            return true;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User is null)
        {
            return false;
        }

        var user = httpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(authorizeAttribute.Roles))
        {
            var requiredRoles = authorizeAttribute.Roles.Split(',').Select(r => r.Trim());
            var userHasRequiredRole = requiredRoles.Any(user.IsInRole);

            if (!userHasRequiredRole)
            {
                return false;
            }
        }

        return true;
    }
}
