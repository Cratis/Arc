// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authentication;

/// <summary>
/// Represents the result of an authentication attempt.
/// </summary>
/// <param name="Principal">The authenticated principal.</param>
/// <param name="Failure">The authentication failure.</param>
public record AuthenticationResult(ClaimsPrincipal? Principal = default, AuthenticationFailure? Failure = default)
{
    /// <summary>
    /// Gets an anonymous authentication result.
    /// </summary>
    public static readonly AuthenticationResult Anonymous = new();

    /// <summary>
    /// Gets the authenticated principal.
    /// </summary>
    public bool IsAuthenticated => Principal is not null;

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    /// <param name="reason">The reason for the authentication failure.</param>
    /// <returns>The failed <see cref="AuthenticationResult"/>.</returns>
    public static AuthenticationResult Failed(AuthenticationFailureReason reason) => new(Failure: new(reason));

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The successful <see cref="AuthenticationResult"/>.</returns>
    public static AuthenticationResult Succeeded(ClaimsPrincipal principal) => new(Principal: principal);
}