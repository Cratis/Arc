// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.given;

public sealed class RecordingUnitOfWork : IUnitOfWork
{
    readonly List<object> _events = [];
    public Action<EventSequenceId, EventForEventSourceId[], KeyValuePair<EventSourceId, ConcurrencyScope>[]>? Enrollment { get; init; }
    public bool IsCompleted { get; private set; }
    public CorrelationId CorrelationId => CorrelationId.NotSet;
    public bool IsSuccess => !Violations.Any();
    public IEnumerable<ConcurrencyViolation> Violations { get; set; } = [];
    public int CommitCount { get; private set; }

    public void AddEvent(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, Causation causation, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, ConcurrencyScope? concurrencyScope = default, IEnumerable<string>? tags = default, DateTimeOffset? occurred = default, Subject? subject = default) =>
        AddEvents(eventSequenceId, [new(eventSourceId, @event, causation)], [new KeyValuePair<EventSourceId, ConcurrencyScope>(eventSourceId, concurrencyScope ?? ConcurrencyScope.NotSet)]);

    public void AddEvents(EventSequenceId eventSequenceId, IEnumerable<EventForEventSourceId> events, IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
    {
        var snapshot = events.ToArray();
        _events.AddRange(snapshot.Select(@event => @event.Event));
        Enrollment?.Invoke(eventSequenceId, snapshot, concurrencyScopes.ToArray());
    }

    public IEnumerable<object> GetEvents() => _events;
    public IEnumerable<ConstraintViolation> GetConstraintViolations() => [];
    public IEnumerable<ConcurrencyViolation> GetConcurrencyViolations() => Violations;
    public IEnumerable<AppendError> GetAppendErrors() => [];
    public Task Commit()
    {
        CommitCount++;
        IsCompleted = true;
        return Task.CompletedTask;
    }

    public Task Rollback()
    {
        _events.Clear();
        IsCompleted = true;
        return Task.CompletedTask;
    }

    public void OnCompleted(Action<IUnitOfWork> callback) { }
    public bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber)
    {
        eventSequenceNumber = null;
        return false;
    }

    public void Dispose() { }
}
