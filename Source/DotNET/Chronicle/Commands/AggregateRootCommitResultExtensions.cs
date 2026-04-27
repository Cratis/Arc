// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Extension methods for converting <see cref="AggregateRootCommitResult"/> to command results.
/// </summary>
public static class AggregateRootCommitResultExtensions
{
    /// <summary>
    /// Converts an <see cref="AggregateRootCommitResult"/> to a <see cref="Arc.Commands.CommandResult"/>,
    /// mapping constraint violations, concurrency violations, append errors, and aggregate-reported
    /// validation results into the returned <see cref="Arc.Commands.CommandResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="AggregateRootCommitResult"/> to convert.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> to assign to the result.</param>
    /// <returns>A <see cref="Arc.Commands.CommandResult"/> representing the outcome.</returns>
    public static Arc.Commands.CommandResult ToCommandResult(this AggregateRootCommitResult result, CorrelationId correlationId)
    {
        var validationResults = new List<ValidationResult>(result.ValidationResults);

        if (result.ConstraintViolations.Any())
        {
            validationResults.AddRange(result.ConstraintViolations.Select(v =>
                ValidationResult.Error(v.Message.Value)));
        }

        if (result.ConcurrencyViolations.Any())
        {
            validationResults.AddRange(result.ConcurrencyViolations.Select(v =>
                ValidationResult.Error(
                    $"Concurrency violation for event source {v.EventSourceId}: Expected sequence number {v.ExpectedEventSequenceNumber}, but actual is {v.ActualEventSequenceNumber}")));
        }

        return new Arc.Commands.CommandResult
        {
            CorrelationId = correlationId,
            ValidationResults = validationResults,
            ExceptionMessages = result.Errors.Any() ? result.Errors.Select(e => e.Value) : []
        };
    }
}
