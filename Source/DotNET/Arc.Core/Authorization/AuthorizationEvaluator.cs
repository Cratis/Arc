// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Http;
using Cratis.Types;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Helper class for performing authorization checks.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> to access the current HTTP request context.</param>
/// <param name="systemExecutionAccessor">The <see cref="ISystemExecutionAccessor"/> to access the current server-side execution principal.</param>
/// <param name="anonymousEvaluators">The collection of <see cref="IAnonymousEvaluator"/> instances.</param>
/// <param name="authorizationAttributeEvaluators">The collection of <see cref="IAuthorizationAttributeEvaluator"/> instances.</param>
public class AuthorizationEvaluator(
    IHttpRequestContextAccessor httpRequestContextAccessor,
    ISystemExecutionAccessor systemExecutionAccessor,
    IInstancesOf<IAnonymousEvaluator> anonymousEvaluators,
    IInstancesOf<IAuthorizationAttributeEvaluator> authorizationAttributeEvaluators) : IAuthorizationEvaluator
{
    /// <inheritdoc/>
    public bool IsAuthorized(Type type)
    {
        // Check all anonymous evaluators first
        foreach (var evaluator in anonymousEvaluators)
        {
            var result = evaluator.IsAnonymousAllowed(type);
            if (result.HasValue)
            {
                if (result.Value)
                {
                    return true;
                }

                // If any evaluator explicitly denies (returns false), we continue checking authorization
                break;
            }
        }

        // Check all authorization attribute evaluators
        var hasAuthorize = false;
        string? roles = null;
        foreach (var evaluator in authorizationAttributeEvaluators)
        {
            var authInfo = evaluator.GetAuthorizationInfo(type);
            if (authInfo.HasValue && authInfo.Value.HasAuthorize)
            {
                hasAuthorize = true;
                roles = authInfo.Value.Roles;
                break;
            }
        }

        return IsAuthorizedWithRoles(hasAuthorize, roles);
    }

    /// <inheritdoc/>
    public bool IsAuthorized(MethodInfo method)
    {
        // Check all anonymous evaluators first
        foreach (var evaluator in anonymousEvaluators)
        {
            var result = evaluator.IsAnonymousAllowed(method);
            if (result.HasValue)
            {
                if (result.Value)
                {
                    return true;
                }

                // If any evaluator explicitly denies (returns false), we continue checking authorization
                break;
            }
        }

        // Check all authorization attribute evaluators for the method
        var hasAuthorize = false;
        string? roles = null;
        foreach (var evaluator in authorizationAttributeEvaluators)
        {
            var authInfo = evaluator.GetAuthorizationInfo(method);
            if (authInfo.HasValue && authInfo.Value.HasAuthorize)
            {
                hasAuthorize = true;
                roles = authInfo.Value.Roles;
                break;
            }
        }

        if (hasAuthorize)
        {
            return IsAuthorizedWithRoles(hasAuthorize, roles);
        }

        var declaringType = method.DeclaringType;
        if (declaringType is not null)
        {
            return IsAuthorized(declaringType);
        }

        return true;
    }

    bool IsAuthorizedWithRoles(bool hasAuthorize, string? roles)
    {
        if (!hasAuthorize)
        {
            return true;
        }

        // On any HTTP request the request principal is authoritative — the server-side execution
        // principal is consulted only when there is no HTTP request context, so it can never
        // influence authorization of an HTTP-origin command.
        var user = httpRequestContextAccessor.Current is not null
            ? httpRequestContextAccessor.Current.User
            : systemExecutionAccessor.Current;
        if (user is null)
        {
            return false;
        }

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(roles))
        {
            var requiredRoles = roles.Split(',').Select(r => r.Trim());
            var userHasRequiredRole = requiredRoles.Any(user.IsInRole);

            if (!userHasRequiredRole)
            {
                return false;
            }
        }

        return true;
    }
}
