// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public async Task<bool> AuthenticateAsync(IHttpRequestContext context, EndpointMetadata? metadata)
    {
        if (metadata?.AllowAnonymous == true)
        {
            return true;
        }

        var result = await authentication.HandleAuthentication(context);

        if (!result.IsAuthenticated)
        {
            context.SetStatusCode(401);
            await context.WriteAsync("Unauthorized", context.RequestAborted);
            return false;
        }

        return true;
    }
}
