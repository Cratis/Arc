// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels;

internal sealed partial class CommandDecisionReads
{
    /// <inheritdoc/>
    public DecisionReadAdmission AdmitForStream<T>()
        where T : class => Admit<T>(true);

    /// <inheritdoc/>
    public Task<DecisionRead<T>> Get<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class => Get<T>(eventSourceId.Value, new(eventSourceId, eventStreamType, eventStreamId, eventSourceType), null, cancellationToken);

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetFor<T>(IUnitOfWork unitOfWork, EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class => Get<T>(eventSourceId.Value, new(eventSourceId, eventStreamType, eventStreamId, eventSourceType), unitOfWork, cancellationToken);

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class
    {
        ReadProtectionMeasurement.Current?.DecisionRead<T>(inner, eventSourceId, eventStreamType, eventStreamId, eventSourceType, detached: true);
        RefuseProtectedDetachedRead();

        return inner.GetDetached<T>(eventSourceId, eventStreamType, eventStreamId, eventSourceType, cancellationToken);
    }

    Task<DecisionRead<T>> ReadAmbient<T>(ReadModelKey key, StreamScope? scope, CancellationToken cancellationToken)
        where T : class => scope is null
            ? inner.Get<T>(key, cancellationToken)
            : inner.Get<T>(scope.EventSourceId, scope.EventStreamType, scope.EventStreamId, scope.EventSourceType, cancellationToken);

    Task<DecisionRead<T>> ReadDetached<T>(ReadModelKey key, StreamScope? scope, CancellationToken cancellationToken)
        where T : class => scope is null
            ? inner.GetDetached<T>(key, cancellationToken)
            : inner.GetDetached<T>(scope.EventSourceId, scope.EventStreamType, scope.EventStreamId, scope.EventSourceType, cancellationToken);

    Task<DecisionRead<T>> ReadEnrolled<T>(IUnitOfWork unit, ReadModelKey key, StreamScope? scope, CancellationToken cancellationToken)
        where T : class => scope is null
            ? inner.GetFor<T>(unit, key, cancellationToken)
            : inner.GetFor<T>(unit, scope.EventSourceId, scope.EventStreamType, scope.EventStreamId, scope.EventSourceType, cancellationToken);

    async Task<DecisionRead<T>> ReadAdvisory<T>(ReadModelKey key, StreamScope? scope, CancellationToken cancellationToken)
        where T : class
    {
        if (scope is null)
        {
            return DecisionRead<T>.Unprotected(key, await ReadLegacy<T>(key));
        }

        // A legacy whole-source read cannot stand in for an explicit stream fold. Keep the stream's instance key,
        // but discard its guard: an opted-out command must not enroll a protected decision.
        var read = await ReadDetached<T>(key, scope, cancellationToken);

        return DecisionRead<T>.Unprotected(read.Key, read.Instance);
    }

    sealed record StreamScope(EventSourceId EventSourceId, EventStreamType EventStreamType, EventStreamId EventStreamId, EventSourceType? EventSourceType);
}
