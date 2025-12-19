// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Defines the system that handles <see cref="IdentityProviderResult"/>.
/// </summary>
public interface IIdentityProviderResultHandler
{
    /// <summary>
    /// Generates an <see cref="IdentityProviderResult"/> from the current HTTP context.
    /// </summary>
    /// <returns>The <see cref="IdentityProviderResult"/>.</returns>
    Task<IdentityProviderResult> GenerateFromCurrentContext();

    /// <summary>
    /// Writes the <see cref="IdentityProviderResult"/> to the response.
    /// </summary>
    /// <param name="result">The <see cref="IdentityProviderResult"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Write(IdentityProviderResult result);

    /// <summary>
    /// Modifies the details of the identity stored in the identity cookie.
    /// </summary>
    /// <typeparam name="TDetails">Type of the details.</typeparam>
    /// <param name="details">Function to modify the details.</param>
    /// <returns>Awaitable task.</returns>
    Task ModifyDetails<TDetails>(Func<TDetails, TDetails> details);
}
