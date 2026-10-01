// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Defines the system that handles <see cref="IdentityProviderResult"/>.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Generates an <see cref="IdentityProviderResult"/> from the authenticated principal of the current HTTP context.
    /// </summary>
    /// <remarks>
    /// The identity is always derived from the authenticated request. Nothing the client sends besides its
    /// credentials, such as an identity cookie, is read.
    /// </remarks>
    /// <returns>The <see cref="IdentityProviderResult"/>.</returns>
    Task<IdentityProviderResult> Get();

    /// <summary>
    /// Gets an <see cref="IdentityProviderResult{TDetails}"/> from the authenticated principal of the current HTTP context.
    /// </summary>
    /// <typeparam name="TDetails">Type of the details.</typeparam>
    /// <returns>The <see cref="IdentityProviderResult{TDetails}"/>.</returns>
    Task<IdentityProviderResult<TDetails>> Get<TDetails>();

    /// <summary>
    /// Writes the <see cref="IdentityProviderResult"/> to the response body as JSON, with headers that keep it out of shared caches.
    /// </summary>
    /// <remarks>
    /// Despite its name, this no longer writes an identity cookie. The name is kept for compatibility.
    /// </remarks>
    /// <param name="result">The <see cref="IdentityProviderResult"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task SetCookieForHttpResponse(IdentityProviderResult result);

    /// <summary>
    /// Modifies the details of the current identity and writes the result to the response.
    /// </summary>
    /// <remarks>
    /// The modification applies to the current response only. It is not stored: the next request derives the
    /// identity from the authenticated principal again.
    /// </remarks>
    /// <typeparam name="TDetails">Type of the details.</typeparam>
    /// <param name="details">Function to modify the details.</param>
    /// <returns>Awaitable task.</returns>
    Task ModifyDetails<TDetails>(Func<TDetails, TDetails> details);
}
