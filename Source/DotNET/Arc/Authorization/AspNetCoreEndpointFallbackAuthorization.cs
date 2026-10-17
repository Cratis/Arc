// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Applies ASP.NET Core's fallback policy to individual queries on the anonymous query hub.
/// </summary>
public class AspNetCoreEndpointFallbackAuthorization : IEndpointFallbackAuthorization
{
    /// <inheritdoc/>
    public async Task<bool> IsAuthorized(IHttpRequestContext context, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var services = context.RequestServices;
        cancellationToken.ThrowIfCancellationRequested();
        var provider = services.GetService<IAuthorizationPolicyProvider>();
        var policy = provider is null ? null : await provider.GetFallbackPolicyAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (policy is null)
        {
            return true;
        }

        var httpContext = services.GetService<IHttpContextAccessor>()?.HttpContext;
        if (httpContext is null)
        {
            // A captured identity cannot authenticate a policy's named schemes without the live request.
            if (policy.AuthenticationSchemes.Count > 0)
            {
                return false;
            }

            var result = await services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, context, policy);
            cancellationToken.ThrowIfCancellationRequested();
            return result.Succeeded;
        }

        var evaluator = services.GetRequiredService<IPolicyEvaluator>();
        var authentication = await evaluator.AuthenticateAsync(policy, httpContext);
        cancellationToken.ThrowIfCancellationRequested();
        var authorization = await evaluator.AuthorizeAsync(policy, authentication, httpContext, httpContext);
        cancellationToken.ThrowIfCancellationRequested();
        return authorization.Succeeded;
    }
}
