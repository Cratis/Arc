// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the actual endpoint called for identity details (/.cratis/me).
/// </summary>
/// <param name="identityProviderResultHandler"><see cref="IIdentityProviderResultHandler"/> for handling the identity provider result.</param>
public class IdentityProviderEndpoint(IIdentityProviderResultHandler identityProviderResultHandler)
{
    /// <summary>
    /// Handle the identity request.
    /// </summary>
    /// <param name="response"><see cref="HttpResponse"/> that will be written to.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handler(HttpResponse response)
    {
        var result = await identityProviderResultHandler.GenerateFromCurrentContext();
        if (!result.IsAuthenticated)
        {
            response.StatusCode = 401;
            return;
        }

        if (!result.IsAuthorized)
        {
            response.StatusCode = 403;
            return;
        }
        await identityProviderResultHandler.Write(result);
    }
}
