// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents the result of committing an <see cref="IAggregateRoot"/>.
/// </summary>
public class AggregateRootCommitResult
{
    /// <summary>
    /// Gets list of committed events ordered by their sequence number.
    /// </summary>
    public IEnumerable<object> Events { get; init; } = [];

    /// <summary>
    /// Gets the sequence numbers assigned to each committed event, in the same order as <see cref="Events"/>.
    /// </summary>
    public IEnumerable<EventSequenceNumber> SequenceNumbers { get; init; } = [];

    /// <summary>
    /// Gets the constraint violations that occurred during the commit.
    /// </summary>
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets the concurrency violations that occurred during the commit.
    /// </summary>
    public IEnumerable<ConcurrencyViolation> ConcurrencyViolations { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the commit was successful.
    /// </summary>
    public bool IsSuccess => !ConstraintViolations.Any() && !ConcurrencyViolations.Any() && !Errors.Any();

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Implicitly convert from <see cref="AggregateRootCommitResult"/> to <see cref="bool"/>.
    /// </summary>
    /// <param name="result">The <see cref="AggregateRootCommitResult"/>.</param>
    public static implicit operator bool(AggregateRootCommitResult result) => result.IsSuccess;

    /// <summary>
    /// Create a successful <see cref="AggregateRootCommitResult"/>.
    /// </summary>
    /// <param name="events">Collection of events.</param>
    /// <param name="sequenceNumbers">Optional collection of <see cref="EventSequenceNumber"/> assigned to each event.</param>
    /// <returns><see cref="AggregateRootCommitResult"/>.</returns>
    public static AggregateRootCommitResult Successful(IImmutableList<object>? events = default, IEnumerable<EventSequenceNumber>? sequenceNumbers = default) =>
        new()
        {
            Events = events ?? ImmutableList<object>.Empty,
            SequenceNumbers = sequenceNumbers ?? []
        };

    /// <summary>
    /// Create an <see cref="AggregateRootCommitResult"/> from <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/> to create from.</param>
    /// <param name="sequenceNumbers">The <see cref="EventSequenceNumber"/> values assigned to each committed event, in order.</param>
    /// <returns>A new instance of <see cref="AggregateRootCommitResult"/>.</returns>
    public static AggregateRootCommitResult CreateFrom(IUnitOfWork unitOfWork, IEnumerable<EventSequenceNumber> sequenceNumbers) =>
        new()
        {
            Events = unitOfWork.GetEvents().ToArray(),
            SequenceNumbers = sequenceNumbers.ToArray(),
            ConstraintViolations = unitOfWork.GetConstraintViolations().ToArray(),
            ConcurrencyViolations = unitOfWork.GetConcurrencyViolations().ToArray(),
            Errors = unitOfWork.GetAppendErrors().ToArray()
        };
}
