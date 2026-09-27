// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http;

/// <summary>
/// Middleware for authenticating HTTP requests.
/// </summary>
/// <param name="authentication">The authentication service.</param>
public class AuthenticationMiddleware(IAuthentication authentication)
{
    /// <summary>
    /// Handles authentication for the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <param name="metadata">The endpoint metadata.</param>
    /// <returns>True if the request is authenticated or allows anonymous access, false otherwise.</returns>
    /// <exception cref="AuthenticationRequiredWithoutHandlers">Authentication is required but no handler is registered.</exception>
    public async Task<bool> Authenticate(IHttpRequestContext context, EndpointMetadata? metadata)
    {
        if (!authentication.HasHandlers)
        {
            if (metadata?.RequireAuthentication == true)
            {
                throw new AuthenticationRequiredWithoutHandlers(metadata.Name);
            }
            return true;
        }

        context.SetEndpointMetadata(metadata);

        var result = await authentication.HandleAuthentication(context);

        if (result.IsAuthenticated)
        {
            context.User = result.Principal!;
        }

        // Anonymous endpoints are always allowed through, even without valid credentials.
        if (metadata?.AllowAnonymous == true)
        {
            return true;
        }

        if (!result.IsAuthenticated)
        {
            context.SetStatusCode(HttpStatusCode.Unauthorized);
            await context.Write("Unauthorized", context.RequestAborted);
            return false;
        }

        if (metadata?.Roles is { } roles && !roles.Split(',').Any(role => context.User.IsInRole(role.Trim())))
        {
            context.SetStatusCode(HttpStatusCode.Forbidden);
            await context.Write("Forbidden", context.RequestAborted);
            return false;
        }

        return true;
    }
}
