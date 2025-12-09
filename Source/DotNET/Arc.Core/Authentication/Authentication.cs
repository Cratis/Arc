// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Types;

namespace Cratis.Arc.Authentication;

/// <summary>
/// Represents the authentication service.
/// </summary>
/// <param name="handlers">The authentication handlers.</param>
public class Authentication(IInstancesOf<IAuthenticationHandler> handlers) : IAuthentication
{
    /// <inheritdoc/>
    public async Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        foreach (var handler in handlers)
        {
            var result = await handler.HandleAuthentication(context);
            if (result.IsAuthenticated || result.Failure is not null)
            {
                return result;
            }
        }

        return AuthenticationResult.Anonymous;
    }
}