// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
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
    /// source raced, without parsing it back out of the prose.
    /// </remarks>
    public static ValidationResult ToValidationResult(this ConcurrencyViolation violation) =>
        ValidationResult.Error(
            $"Concurrency violation for event source {violation.EventSourceId}: Expected sequence number {violation.ExpectedEventSequenceNumber}, but actual is {violation.ActualEventSequenceNumber}",
            state: violation,
            reason: ValidationResultReason.ConcurrencyViolation);
}
