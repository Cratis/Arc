// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// A single operation's resolved policies and authentication schemes, held only for the duration of admission.
/// </summary>
public interface IAuthorizationPolicyResolution
{
    /// <summary>
    /// Selects the principal from the schemes resolved for this operation.
    /// </summary>
    /// <param name="principal">The default principal.</param>
    /// <param name="services">The executing scope.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The selected principal, or null when none of the requested schemes authenticate.</returns>
    Task<ClaimsPrincipal?> SelectPrincipal(ClaimsPrincipal? principal, IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates the same policies used to select authentication schemes.
    /// </summary>
    /// <param name="context">The selected principal and resource.</param>
    /// <param name="services">The executing scope.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether all requirements are satisfied.</returns>
    Task<bool> IsAuthorized(AuthorizationPolicyContext context, IServiceProvider services, CancellationToken cancellationToken);
}
