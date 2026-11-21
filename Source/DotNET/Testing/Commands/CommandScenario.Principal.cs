// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Testing.Commands;

public partial class CommandScenario<TCommand>
{
    /// <summary>
    /// Sets the principal used by authorization and caller-derived dependencies during execution and validation.
    /// </summary>
    /// <param name="principal">The principal to execute as.</param>
    /// <returns>This scenario for further configuration.</returns>
    /// <exception cref="ObjectDisposedException">The scenario has been disposed.</exception>
    /// <exception cref="ScenarioPrincipalMustBeConfiguredBeforeInitialization">Execute or Validate has already initialized the scenario.</exception>
    public CommandScenario<TCommand> WithPrincipal(ClaimsPrincipal principal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(principal);
        if (IsInitialized)
        {
            throw new ScenarioPrincipalMustBeConfiguredBeforeInitialization();
        }

        Services.RemoveAll<ICurrentPrincipalAccessor>();
        Services.AddSingleton<ICurrentPrincipalAccessor>(new ScenarioPrincipal(principal));

        return this;
    }

    /// <summary>
    /// Sets an authenticated principal carrying the given claims before execution or validation.
    /// </summary>
    /// <param name="claims">The caller's claims.</param>
    /// <returns>This scenario for further configuration.</returns>
    public CommandScenario<TCommand> WithClaims(params Claim[] claims) =>
        WithPrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "CommandScenario")));

    /// <summary>
    /// Sets an anonymous principal before execution or validation.
    /// </summary>
    /// <returns>This scenario for further configuration.</returns>
    public CommandScenario<TCommand> Unauthenticated() => WithPrincipal(new ClaimsPrincipal(new ClaimsIdentity()));

    sealed class ScenarioPrincipal(ClaimsPrincipal principal) : ICurrentPrincipalAccessor
    {
        public ClaimsPrincipal Current => principal;
    }
}
