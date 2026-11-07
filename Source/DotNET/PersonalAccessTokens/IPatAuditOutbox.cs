// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>
/// Internal delivery port for immutable audit snapshots committed in the authority's local transaction.
/// This is not an authoritative credential lookup and must never participate in verification or revocation.
/// </summary>
internal interface IPatAuditOutbox
{
    /// <summary>Reads a bounded batch of unacknowledged intents in commit order, with stable identifiers on retry.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Secret-free snapshots; never reconstruct these from mutable credential state during delivery.</returns>
    Task<IReadOnlyList<PatAuditIntent>> ReadPending(CancellationToken cancellationToken);

    /// <summary>Acknowledges a successfully appended intent, idempotently.</summary>
    /// <param name="intentId">The stable transactionally assigned intent identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The acknowledgement task.</returns>
    Task Acknowledge(Guid intentId, CancellationToken cancellationToken);
}
