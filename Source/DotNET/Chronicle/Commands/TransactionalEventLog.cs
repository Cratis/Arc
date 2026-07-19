// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an <see cref="IEventLog"/> that enrolls appends in the command's <see cref="IUnitOfWork"/> when one is
/// active, so every event a command appends is committed atomically with the command and rolled back if the command
/// fails. When no unit of work is current (for example outside a command), appends fall through to the underlying log
/// immediately.
/// </summary>
/// <param name="inner">The underlying <see cref="IEventLog"/> that performs the actual appends and reads.</param>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> used to resolve the ambient unit of work.</param>
public class TransactionalEventLog(IEventLog inner, IUnitOfWorkManager unitOfWorkManager) : IEventLog
{
    /// <summary>
    /// The causation property carrying the event sequence id.
    /// </summary>
    public const string CausationEventSequenceIdProperty = "eventSequenceId";

    /// <summary>
    /// The causation type recorded for events a command appends.
    /// </summary>
    public static readonly CausationType CausationType = "Command";

    /// <inheritdoc/>
    public EventSequenceId Id => inner.Id;

    /// <inheritdoc/>
    public IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations => inner.AppendOperations;

    /// <inheritdoc/>
    public ITransactionalEventSequence Transactional => inner.Transactional;

    /// <inheritdoc/>
    public Task<AppendResult> Append(EventSourceId eventSourceId, object @event, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, EventSourceType? eventSourceType = null, CorrelationId? correlationId = null, IEnumerable<string>? tags = null, ConcurrencyScope? concurrencyScope = null, DateTimeOffset? occurred = null, Subject? subject = null)
    {
        if (!unitOfWorkManager.HasCurrent)
        {
            return inner.Append(eventSourceId, @event, eventStreamType, eventStreamId, eventSourceType, correlationId, tags, concurrencyScope, occurred, subject);
        }

        var unitOfWork = unitOfWorkManager.Current;
        unitOfWork.AddEvent(inner.Id, eventSourceId, @event, CreateCausation(), eventStreamType, eventStreamId, eventSourceType, concurrencyScope, tags, occurred, subject);
        return Task.FromResult(AppendResult.Success(unitOfWork.CorrelationId, EventSequenceNumber.Unavailable));
    }

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(EventSourceId eventSourceId, IEnumerable<object> events, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, EventSourceType? eventSourceType = null, CorrelationId? correlationId = null, IEnumerable<string>? tags = null, ConcurrencyScope? concurrencyScope = null, DateTimeOffset? occurred = null, Subject? subject = null)
    {
        if (!unitOfWorkManager.HasCurrent)
        {
            return inner.AppendMany(eventSourceId, events, eventStreamType, eventStreamId, eventSourceType, correlationId, tags, concurrencyScope, occurred, subject);
        }

        var unitOfWork = unitOfWorkManager.Current;
        foreach (var @event in events)
        {
            unitOfWork.AddEvent(inner.Id, eventSourceId, @event, CreateCausation(), eventStreamType, eventStreamId, eventSourceType, concurrencyScope, tags, occurred, subject);
        }

        return Task.FromResult(new AppendManyResult { CorrelationId = unitOfWork.CorrelationId });
    }

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(IEnumerable<EventForEventSourceId> events, CorrelationId? correlationId = null, IEnumerable<string>? tags = null, IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = null)
    {
        if (!unitOfWorkManager.HasCurrent)
        {
            return inner.AppendMany(events, correlationId, tags, concurrencyScopes);
        }

        var unitOfWork = unitOfWorkManager.Current;
        foreach (var @event in events)
        {
            var concurrencyScope = concurrencyScopes is not null && concurrencyScopes.TryGetValue(@event.EventSourceId, out var scope) ? scope : null;
            unitOfWork.AddEvent(inner.Id, @event.EventSourceId, @event.Event, CreateCausation(), concurrencyScope: concurrencyScope, tags: tags);
        }

        return Task.FromResult(new AppendManyResult { CorrelationId = unitOfWork.CorrelationId });
    }

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

    Causation CreateCausation() =>
        new(DateTimeOffset.Now, CausationType, new Dictionary<string, string> { { CausationEventSequenceIdProperty, inner.Id } });
}
