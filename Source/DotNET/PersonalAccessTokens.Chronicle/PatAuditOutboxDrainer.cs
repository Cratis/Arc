// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.PersonalAccessTokens.Chronicle;

/// <summary>Delivers audit-only snapshots; never reads or updates authoritative credential status.</summary>
/// <param name="outbox">The authority's transactionally committed audit outbox.</param>
/// <param name="eventLog">An event log supporting durable idempotent appends.</param>
internal sealed class PatAuditOutboxDrainer(IPatAuditOutbox outbox, IEventLog eventLog)
{
    /// <summary>Drains one bounded batch, acknowledging only successful appends.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the batch was delivered successfully; false leaves pending intents for retry.</returns>
    public async Task<bool> Drain(CancellationToken cancellationToken)
    {
        if (!eventLog.SupportsIdempotentAppends)
        {
            return false;
        }

        foreach (var intent in await outbox.ReadPending(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await eventLog.AppendManyOnce(
                intent.Metadata.CredentialId.Value,
                [ToEvent(intent)],
                new IdempotencyKey($"arc:pat:audit:v1:{intent.Id:N}"));
            if (!result.IsSuccess)
            {
                return false;
            }

            await outbox.Acknowledge(intent.Id, cancellationToken);
        }

        return true;
    }

    static object ToEvent(PatAuditIntent intent)
    {
        var data = intent.Metadata;
        return intent.IsRevocation
            ? new PatRevoked(data.CredentialId, data.Resource, data.Environment, data.Tenant, data.Person, data.Scopes, data.ExpiresAt, data.Assurance, data.RestoreEpoch, data.VerifierId, intent.OccurredAt)
            : new PatIssued(data.CredentialId, data.Resource, data.Environment, data.Tenant, data.Person, data.Scopes, data.ExpiresAt, data.Assurance, data.RestoreEpoch, data.VerifierId, intent.OccurredAt);
    }
}
