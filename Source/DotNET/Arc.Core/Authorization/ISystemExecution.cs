// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Defines a system for executing commands from server-side / background code as a trusted system actor.
/// </summary>
/// <remarks>
/// Command authorization normally reads the principal from the current HTTP request. Server-side callers
/// (reactors, hosted services, sagas, or one command orchestrating another) have no HTTP request, so any
/// command carrying <see cref="AuthorizeAttribute"/> or <see cref="RolesAttribute"/> is denied. Entering a
/// scope established through this system makes an authenticated principal ambiently available for the
/// duration of the scope, so such commands can execute as a trusted system actor.
/// The established principal is consulted only when there is no HTTP request context, so it can never
/// influence authorization of an HTTP-origin command.
/// </remarks>
public interface ISystemExecution
{
    /// <summary>
    /// Establishes a scope that executes as an authenticated system actor carrying the given roles.
    /// </summary>
    /// <param name="roles">The roles the system actor holds. When empty, the actor satisfies <see cref="AuthorizeAttribute"/> but no <see cref="RolesAttribute"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous execution context when disposed.</returns>
    IDisposable AsSystem(params string[] roles);

    /// <summary>
    /// Establishes a scope that executes as the specified <see cref="ClaimsPrincipal"/>.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to execute as.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous execution context when disposed.</returns>
    IDisposable As(ClaimsPrincipal principal);
}
