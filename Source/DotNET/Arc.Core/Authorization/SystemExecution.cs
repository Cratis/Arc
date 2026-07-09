// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="ISystemExecution"/> that establishes the server-side principal
/// through the <see cref="ICurrentPrincipalOverride"/>.
/// </summary>
/// <param name="currentPrincipalOverride">The <see cref="ICurrentPrincipalOverride"/> used to establish the server-side principal.</param>
public class SystemExecution(ICurrentPrincipalOverride currentPrincipalOverride) : ISystemExecution
{
    /// <inheritdoc/>
    public IDisposable AsSystem(params string[] roles) => As(SystemPrincipal.WithRoles(roles));

    /// <inheritdoc/>
    public IDisposable As(ClaimsPrincipal principal) => currentPrincipalOverride.BeginScope(principal);
}
