// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Resolves policy and authentication scheme requirements for the active host.
/// </summary>
public interface IAuthorizationPolicyRuntime
{
    /// <summary>
    /// Validates every requirement before admission or startup.
    /// </summary>
    /// <param name="requirements">The requirements.</param>
    /// <param name="services">The current service scope.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The validation operation.</returns>
    Task Validate(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves and validates policies and schemes once for an admission attempt.
    /// </summary>
    /// <param name="requirements">The effective requirements.</param>
    /// <param name="services">The executing service scope.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A stable operation-local policy resolution.</returns>
    Task<IAuthorizationPolicyResolution> Resolve(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>
    /// Makes the selected identity visible to the active host request for the duration of authorized execution.
    /// </summary>
    /// <param name="principal">The selected identity.</param>
    /// <param name="services">The executing scope.</param>
    /// <returns>A scope restoring the previous host identity, if the host has one.</returns>
    IDisposable? BeginPrincipalScope(ClaimsPrincipal principal, IServiceProvider services) => null;
}
