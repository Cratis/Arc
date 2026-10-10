// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Gives Castle abstract invocation targets for Chronicle's default interface methods.
/// </summary>
public abstract class UnitOfWorkWithNamedTags : IUnitOfWork
{
    public abstract bool IsCompleted { get; }
    public abstract CorrelationId CorrelationId { get; }
    public abstract bool IsSuccess { get; }
    public abstract bool HasEnrolledDecisionReads { get; }
    public abstract void AddEvent(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, Causation causation, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, ConcurrencyScope? concurrencyScope = default, IEnumerable<string>? tags = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract void AddEventWithNamedTags(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, IEnumerable<NamedTag> namedTags, Causation causation, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, ConcurrencyScope? concurrencyScope = default, IEnumerable<string>? tags = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract void AddEvents(EventSequenceId eventSequenceId, IEnumerable<EventForEventSourceId> events, IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes);
    public abstract void AddDecisionRead(IDecisionRead read);
    public abstract IEnumerable<DecisionConflict> GetDecisionConflicts();
    public abstract IEnumerable<object> GetEvents();
    public abstract IEnumerable<ConstraintViolation> GetConstraintViolations();
    public abstract IEnumerable<ConcurrencyViolation> GetConcurrencyViolations();
    public abstract IEnumerable<AppendError> GetAppendErrors();
    public abstract Task Commit();
    public abstract Task Rollback();
    public abstract void OnCompleted(Action<IUnitOfWork> callback);
    public abstract bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber);
    public abstract void Dispose();
}
