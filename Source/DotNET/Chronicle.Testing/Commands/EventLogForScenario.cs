// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Represents the <see cref="IEventLog"/> the command scenario harness registers: a pure pass-through to the
/// in-memory event log, except <see cref="Transactional"/> is built over the harness's real
/// <see cref="IUnitOfWorkManager"/> — the in-memory log itself carries a manager that does not support units of
/// work, which would make the explicit transactional style throw in tests while it works in production.
/// </summary>
/// <param name="inner">The in-memory <see cref="IEventLog"/> to delegate to.</param>
/// <param name="unitOfWorkManager">The harness's real <see cref="IUnitOfWorkManager"/>.</param>
internal sealed class EventLogForScenario(IEventLog inner, IUnitOfWorkManager unitOfWorkManager) : IEventLog
{
    /// <inheritdoc/>
    public EventSequenceId Id => inner.Id;

    /// <inheritdoc/>
    public IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations => inner.AppendOperations;

    /// <inheritdoc/>
    public ITransactionalEventSequence Transactional => new TransactionalEventSequence(inner, unitOfWorkManager);

    /// <inheritdoc/>
    public Task<AppendResult> Append(EventSourceId eventSourceId, object @event, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, EventSourceType? eventSourceType = null, CorrelationId? correlationId = null, IEnumerable<string>? tags = null, ConcurrencyScope? concurrencyScope = null, DateTimeOffset? occurred = null, Subject? subject = null) =>
        inner.Append(eventSourceId, @event, eventStreamType, eventStreamId, eventSourceType, correlationId, tags, concurrencyScope, occurred, subject);

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(EventSourceId eventSourceId, IEnumerable<object> events, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, EventSourceType? eventSourceType = null, CorrelationId? correlationId = null, IEnumerable<string>? tags = null, ConcurrencyScope? concurrencyScope = null, DateTimeOffset? occurred = null, Subject? subject = null) =>
        inner.AppendMany(eventSourceId, events, eventStreamType, eventStreamId, eventSourceType, correlationId, tags, concurrencyScope, occurred, subject);

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(IEnumerable<EventForEventSourceId> events, CorrelationId? correlationId = null, IEnumerable<string>? tags = null, IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = null) =>
        inner.AppendMany(events, correlationId, tags, concurrencyScopes);

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> filterEventTypes, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, EventSourceType? eventSourceType = null) =>
        inner.GetForEventSourceIdAndEventTypes(eventSourceId, filterEventTypes, eventStreamType, eventStreamId, eventSourceType);

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) => inner.HasEventsFor(eventSourceId);

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = null, IEnumerable<EventType>? filterEventTypes = null) =>
        inner.GetFromSequenceNumber(sequenceNumber, eventSourceId, filterEventTypes);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => inner.GetNextSequenceNumber();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(EventSourceId? eventSourceId = null, EventSourceType? eventSourceType = null, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, IEnumerable<EventType>? filterEventTypes = null) =>
        inner.GetTailSequenceNumber(eventSourceId, eventSourceType, eventStreamType, eventStreamId, filterEventTypes);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => inner.GetTailSequenceNumberForObserver(type);

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason) => inner.Redact(sequenceNumber, reason);

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes) => inner.Redact(eventSourceId, reason, clrEventTypes);

    /// <inheritdoc/>
    public Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId) => inner.CompleteStream(eventStreamType, eventStreamId);
}
