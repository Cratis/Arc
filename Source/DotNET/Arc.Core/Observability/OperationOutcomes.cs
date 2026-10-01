// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Observability;

/// <summary>
/// Maps a command or query result to one of the <see cref="WellKnownOperationOutcomes"/>.
/// </summary>
internal static class OperationOutcomes
{
    /// <summary>
    /// Classifies a command result.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to classify.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> the command ran with.</param>
    /// <returns>The outcome.</returns>
    internal static string For(CommandResult result, CancellationToken cancellationToken) =>
        Classify(result.IsAuthorized, result.HasExceptions, result.ValidationResults, cancellationToken);

    /// <summary>
    /// Classifies a query result.
    /// </summary>
    /// <param name="result">The <see cref="QueryResult"/> to classify.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> the query ran with.</param>
    /// <returns>The outcome.</returns>
    internal static string For(QueryResult result, CancellationToken cancellationToken) =>
        Classify(result.IsAuthorized, result.HasExceptions, result.ValidationResults, cancellationToken);

    /// <summary>
    /// Classifies an operation that ended with an exception instead of a result.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> the operation ran with.</param>
    /// <returns>The outcome.</returns>
    internal static string ForException(CancellationToken cancellationToken) => ErrorOrCancelled(cancellationToken);

    static string Classify(bool isAuthorized, bool hasExceptions, IEnumerable<ValidationResult> validationResults, CancellationToken cancellationToken)
    {
        if (!isAuthorized)
        {
            return WellKnownOperationOutcomes.Authorization;
        }

        if (hasExceptions)
        {
            return ErrorOrCancelled(cancellationToken);
        }

        var results = validationResults as IReadOnlyCollection<ValidationResult> ?? [.. validationResults];
        if (results.Count == 0)
        {
            return WellKnownOperationOutcomes.Success;
        }

        return results.Any(IsAppendRejection) ? WellKnownOperationOutcomes.AppendRejected : WellKnownOperationOutcomes.Validation;
    }

    static string ErrorOrCancelled(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? WellKnownOperationOutcomes.Cancelled : WellKnownOperationOutcomes.Error;

    static bool IsAppendRejection(ValidationResult result) =>
        result.Reason == ValidationResultReason.ConstraintViolation ||
        result.Reason == ValidationResultReason.ConcurrencyViolation;
}
