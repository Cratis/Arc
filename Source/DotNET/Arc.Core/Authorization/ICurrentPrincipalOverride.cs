// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Defines a system for overriding the current principal for server-side execution.
/// </summary>
public interface ICurrentPrincipalOverride
{
    /// <summary>
    /// Begins a scope that overrides the current principal for server-side execution.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to execute as.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous principal when disposed.</returns>
    /// <remarks>
    /// The override is ignored while an HTTP request is in progress — the request principal always wins — so a
    /// server-side scope can never influence authorization of an HTTP-origin command.
    /// </remarks>
    IDisposable BeginScope(ClaimsPrincipal principal);
}
