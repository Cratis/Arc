// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Http;

/// <summary>
/// Applies the host's default endpoint authorization to a query admitted through an anonymous hub route.
/// </summary>
public interface IEndpointFallbackAuthorization
{
    /// <summary>
    /// Determines whether the caller satisfies the host's fallback authorization.
    /// </summary>
    /// <param name="context">The live admission request, not a retained subscription context.</param>
    /// <param name="principal">The caller requesting the subscription.</param>
    /// <param name="cancellationToken">The admission cancellation token.</param>
    /// <returns>Whether the query may proceed to its own authorization pipeline.</returns>
    Task<bool> IsAuthorized(IHttpRequestContext context, ClaimsPrincipal principal, CancellationToken cancellationToken);
}
