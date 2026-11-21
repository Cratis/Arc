// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Delegation;

/// <summary>
/// Product-owned server-side delegation authority. Implementing this seam is not evidence that
/// a grant store, exchange, lineage, budget, revocation, or approval protocol has shipped.
/// </summary>
public interface IDelegationAuthority
{
    /// <summary>
    /// Checks the verified credential's grant or a server-side mapping keyed by its credential id,
    /// never a request-supplied reference. Compares the represented account and current actor,
    /// resource/catalog audience, tenant, environment, and trusted installation binding to server-owned bounds.
    /// Required lineage, attenuation, lifecycle, recovery/revocation fences, and atomic budget checks must
    /// refuse when unavailable. Nested actor history, scopes, client metadata, and possession of a grant id
    /// are not independent authority. Approval and product operation authorization remain separate gates.
    /// </summary>
    /// <param name="request">The verified caller and application-selected capability and destination.</param>
    /// <param name="cancellationToken">Cancellation for the authority check.</param>
    /// <returns>True only when all required current server-owned delegation checks allow the request.</returns>
    Task<bool> IsAllowed(DelegationRequest request, CancellationToken cancellationToken = default);
}
