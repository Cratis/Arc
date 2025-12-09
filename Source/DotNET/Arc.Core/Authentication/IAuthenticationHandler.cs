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
    /// Authenticates the request.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <returns>The result of the authentication attempt.</returns>
    Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context);
}
