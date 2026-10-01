// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Extension methods for converting concurrency violations to validation results.
/// </summary>
public static class ConcurrencyViolationExtensions
{
    /// <summary>
    /// Converts a <see cref="ConcurrencyViolation"/> to a <see cref="ValidationResult"/> error carrying
    /// <see cref="ValidationResultReason.ConcurrencyViolation"/>, and the violation itself as its state.
    /// </summary>
    /// <param name="violation">The <see cref="ConcurrencyViolation"/> to convert.</param>
    /// <returns>A <see cref="ValidationResult"/> representing the violation.</returns>
    /// <remarks>
    /// Chronicle reports the violation as three separate fields, and the message here interpolates them into a
    /// sentence for a developer reading a log. The sentence is not the carrier of the fact: a concurrency violation
    /// is retryable where a rule rejection is not, and the reason is what lets a client offer that retry instead of
    /// pattern-matching English. The violation travels alongside as state so the client can also see which event
    /// source raced, without parsing it back out of the prose. Chronicle's sentinel sequence numbers (for example the
    /// "no events yet" expectation of a first append into a scope) are described in words, not printed as raw numbers.
    /// </remarks>
    public static ValidationResult ToValidationResult(this ConcurrencyViolation violation) =>
        ValidationResult.Error(
            $"Event source '{violation.EventSourceId}' has new events since the command read it: expected {Describe(violation.ExpectedEventSequenceNumber)}, " +
            $"but it has {Describe(violation.ActualEventSequenceNumber)}. Read it again and resubmit.",
            state: violation,
            reason: ValidationResultReason.ConcurrencyViolation);

    static string Describe(EventSequenceNumber sequenceNumber) =>
        sequenceNumber.IsBeforeFirst ? "no events" :
        sequenceNumber.IsActualValue ? $"events up to sequence number {sequenceNumber.Value}" :
        "an unknown position";
}
