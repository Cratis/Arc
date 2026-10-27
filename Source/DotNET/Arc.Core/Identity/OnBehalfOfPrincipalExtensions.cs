// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity;

/// <summary>
/// Extension methods for reading who acts when a principal is used on behalf of another user.
/// </summary>
/// <remarks>
/// In an on-behalf session the principal is the represented user, so roles, tenant and authorization follow that
/// user. The identity that actually acts is the <see cref="ClaimsIdentity.Actor"/> of the principal's primary identity.
/// </remarks>
public static class OnBehalfOfPrincipalExtensions
{
    /// <summary>
    /// Gets the identity acting on behalf of the principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> representing the user acted on behalf of.</param>
    /// <returns>The acting <see cref="ClaimsIdentity"/>, or <see langword="null"/> when the principal acts for itself.</returns>
    public static ClaimsIdentity? GetActor(this ClaimsPrincipal principal) =>
        principal.Identities.FirstOrDefault(identity => identity.Actor is not null)?.Actor;

    /// <summary>
    /// Checks whether another identity is acting on behalf of the principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to check.</param>
    /// <returns>True when an actor is acting on behalf of the principal.</returns>
    public static bool IsActedOnBehalfOf(this ClaimsPrincipal principal) => principal.GetActor() is not null;
}
