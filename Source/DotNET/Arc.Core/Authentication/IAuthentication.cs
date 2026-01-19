// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication;

/// <summary>
/// Defines the authentication service.
/// </summary>
public interface IAuthentication
{
    /// <summary>
    /// Gets a value indicating whether there are any authentication handlers configured.
    /// </summary>
    bool HasHandlers { get; }

    /// <summary>
    /// Handles authentication for the given HTTP request context.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <returns>The result of the authentication attempt.</returns>
    Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context);
}
