// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Provides an operation-local Arc request identity without writing to a shared native HTTP context.
/// </summary>
public interface IAuthorizationRequestContext
{
    /// <summary>
    /// Begins a selected identity and execution provider for the current asynchronous flow.
    /// </summary>
    /// <param name="principal">The selected principal.</param>
    /// <param name="services">The clean execution provider.</param>
    /// <returns>A scope restoring the previous request identity.</returns>
    IDisposable BeginSelectedPrincipal(ClaimsPrincipal principal, IServiceProvider services);
}
