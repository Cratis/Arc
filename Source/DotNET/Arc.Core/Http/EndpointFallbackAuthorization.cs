// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http;

/// <summary>
/// Applies the Arc Core host's authentication requirement when authentication handlers are registered.
/// </summary>
/// <param name="authentication">The host's authentication configuration.</param>
public class EndpointFallbackAuthorization(IAuthentication authentication) : IEndpointFallbackAuthorization
{
    /// <inheritdoc/>
    public Task<bool> IsAuthorized(IHttpRequestContext context, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!authentication.HasHandlers || principal.Identities.Any(identity => identity.IsAuthenticated));
    }
}
