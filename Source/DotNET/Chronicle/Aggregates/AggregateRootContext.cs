// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootContext"/>.
/// </summary>
/// <param name="eventSourceType">The <see cref="EventSourceType"/> for the context.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> for the context.</param>
/// <param name="eventStreamType">The <see cref="EventStreamType"/> for the context.</param>
/// <param name="eventStreamId">The <see cref="EventStreamId"/> for the context.</param>
/// <param name="eventSequence">The <see cref="IEventSequence"/> for the context.</param>
/// <param name="aggregateRoot">The <see cref="IAggregateRoot"/> for the context.</param>
/// <param name="unitOfWork">The <see cref="IUnitOfWork"/> for the context.</param>
/// <param name="nextSequenceNumber">The next <see cref="EventSequenceNumber"/>.</param>
/// <param name="tailSequenceNumber">The tail <see cref="EventSequenceNumber"/> representing the tail of the aggregate roots event stream.</param>
/// <param name="eventSource">The optional type of the event source definition the aggregate root declares.</param>
/// <param name="eventStream">The optional name of the stream declared on the event source.</param>
public class AggregateRootContext(
    EventSourceType eventSourceType,
    EventSourceId eventSourceId,
    EventStreamType eventStreamType,
    EventStreamId eventStreamId,
    IEventSequence eventSequence,
    IAggregateRoot aggregateRoot,
    IUnitOfWork unitOfWork,
    EventSequenceNumber nextSequenceNumber,
    EventSequenceNumber tailSequenceNumber,
    Type? eventSource = default,
    string? eventStream = default) : IAggregateRootContext
{
    /// <inheritdoc/>
    public Type? EventSource { get; } = eventSource;

    /// <inheritdoc/>
    public string? EventStream { get; } = eventStream;

    /// <inheritdoc/>
    public EventSourceType EventSourceType { get; } = eventSourceType;

    /// <inheritdoc/>
    public EventSourceId EventSourceId { get; } = eventSourceId;

    /// <inheritdoc/>
    public EventStreamType EventStreamType { get; } = eventStreamType;

    /// <inheritdoc/>
    public EventStreamId EventStreamId { get; } = eventStreamId;

    /// <inheritdoc/>
    public IEventSequence EventSequence { get; } = eventSequence;

    /// <inheritdoc/>
    public IAggregateRoot AggregateRoot { get; } = aggregateRoot;

    /// <inheritdoc/>
    public IUnitOfWork UnitOfWOrk { get; } = unitOfWork;

    /// <inheritdoc/>
    public EventSequenceNumber NextSequenceNumber { get; set; } = nextSequenceNumber;

    /// <inheritdoc/>
    public EventSequenceNumber TailEventSequenceNumber { get; set; } = tailSequenceNumber;

    /// <inheritdoc/>
    public bool HasEvents { get; set; } = nextSequenceNumber != EventSequenceNumber.First;
}
