// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>
/// Re-evaluates current person, account, tenant and organization status, membership, scope ceilings,
/// upstream SSO freshness and organization PAT policy on every issuance and verification.
/// Recorded policy epochs are hints, not authorization. Missing or unavailable policy refuses.
/// This is a trusted-host security contract, not a sandbox; deny on uncertainty.
/// </summary>
public interface IPatPolicy
{
    /// <summary>Evaluates every current status, membership and SSO requirement.</summary>
    /// <param name="record">Credential metadata.</param>
    /// <param name="issuing">Whether this is an issuance request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A current policy decision.</returns>
    Task<PatPolicyDecision> Evaluate(PatRecord record, bool issuing, CancellationToken cancellationToken);
}
