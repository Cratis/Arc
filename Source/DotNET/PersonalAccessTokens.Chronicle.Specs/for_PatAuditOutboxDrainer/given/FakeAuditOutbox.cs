// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Chronicle.for_PatAuditOutboxDrainer.given;

/// <summary>Hand-written outbox that serves and acknowledges the intents of the owning specification.</summary>
/// <param name="pending">Gets the current pending intents.</param>
/// <param name="acknowledge">Records an acknowledgement.</param>
internal sealed class FakeAuditOutbox(Func<IReadOnlyList<PatAuditIntent>> pending, Action<Guid> acknowledge) : IPatAuditOutbox
{
    /// <summary>Gets the number of times a batch was read.</summary>
    public int ReadCount { get; private set; }

    /// <summary>Gets or sets a value indicating whether acknowledgements fail without being recorded.</summary>
    public bool FailAcknowledgement { get; set; }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PatAuditIntent>> ReadPending(CancellationToken cancellationToken)
    {
        ReadCount++;
        return Task.FromResult(pending());
    }

    /// <inheritdoc/>
    public Task Acknowledge(Guid intentId, CancellationToken cancellationToken)
    {
        if (FailAcknowledgement)
        {
            return Task.FromException(new InvalidOperationException("Acknowledgement unavailable."));
        }

        acknowledge(intentId);
        return Task.CompletedTask;
    }
}
