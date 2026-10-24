// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication;

/// <summary>
/// Defines a handler for authenticating HTTP requests.
/// </summary>
public interface IAuthenticationHandler
{
    /// <summary>
    /// Gets a value indicating whether this handler applies to endpoints that allow anonymous access.
    /// </summary>
    /// <remarks>
    /// An endpoint with <c>AllowAnonymous</c> set does not require a credential; it does not mean an
    /// identity is unwanted. The default is <see langword="true"/> so a handler keeps establishing
    /// <see cref="System.Security.Claims.ClaimsPrincipal"/> on such endpoints unless it explicitly opts
    /// out. A handler that only rejects missing or invalid credentials, and has nothing useful to
    /// contribute on an anonymous endpoint, should override this to <see langword="false"/> rather than
    /// returning <see cref="AuthenticationResult.Failed(AuthenticationFailureReason)"/> for a credential
    /// the endpoint never required.
    /// </remarks>
    bool AppliesToAnonymousEndpoints => true;

    /// <summary>
    /// Authenticates the request.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <returns>The result of the authentication attempt.</returns>
    /// <remarks>
    /// The endpoint's <see cref="EndpointMetadata"/> is already set on <paramref name="context"/> when this method
    /// runs, so a handler can check <see cref="HttpRequestContextEndpointExtensions.AllowsAnonymous"/> before doing
    /// any work. On an endpoint that allows anonymous access, a missing or invalid credential is not an error: a
    /// handler that would otherwise reject the request - and in particular one that logs a rejection - should
    /// return <see cref="AuthenticationResult.Anonymous"/> instead of
    /// <see cref="AuthenticationResult.Failed(AuthenticationFailureReason)"/> in that case.
    /// </remarks>
    Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context);
}
