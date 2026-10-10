// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Gives Castle abstract invocation targets for Chronicle's default interface methods.
/// </summary>
public abstract class EventLogWithNamedTags : IEventLog
{
    public abstract EventSequenceId Id { get; }
    public abstract IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations { get; }
    public abstract ITransactionalEventSequence Transactional { get; }
    public abstract Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> filterEventTypes, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default);
    public abstract Task<bool> HasEventsFor(EventSourceId eventSourceId);
    public abstract Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? filterEventTypes = default);
    public abstract Task<EventSequenceNumber> GetNextSequenceNumber();
    public abstract Task<EventSequenceNumber> GetTailSequenceNumber(EventSourceId? eventSourceId = default, EventSourceType? eventSourceType = default, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, IEnumerable<EventType>? filterEventTypes = default);
    public abstract Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type);
    public abstract Task<AppendResult> Append(EventSourceId eventSourceId, object @event, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract Task<AppendManyResult> AppendMany(EventSourceId eventSourceId, IEnumerable<object> events, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract Task<AppendManyResult> AppendMany(IEnumerable<EventForEventSourceId> events, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default);
    public abstract Task<AppendResult> AppendThroughEventSource(Type eventSource, EventSourceId eventSourceId, object @event, string? eventStream = default, EventStreamId? eventStreamId = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract Task<AppendManyResult> AppendManyThroughEventSource(Type eventSource, EventSourceId eventSourceId, IEnumerable<object> events, string? eventStream = default, EventStreamId? eventStreamId = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract Task<AppendResult> AppendWithNamedTags(EventSourceId eventSourceId, object @event, IEnumerable<NamedTag> namedTags, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract Task<AppendManyResult> AppendManyWithNamedTags(EventSourceId eventSourceId, IEnumerable<object> events, IEnumerable<NamedTag> namedTags, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default);
    public abstract Task<AppendManyResult> AppendManyWithNamedTags(IEnumerable<EventForEventSourceId> events, IEnumerable<NamedTag> namedTags, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default);
    public abstract Task Revise(EventSequenceNumber sequenceNumber, object @event);
    public abstract Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason);
    public abstract Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes);
    public abstract Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId);
}
