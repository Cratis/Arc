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
    /// <returns>The outcome.</returns>
    internal static string For(CommandResult result) => Classify(result.IsAuthorized, result.HasExceptions, result.ValidationResults);

    /// <summary>
    /// Classifies a query result.
    /// </summary>
    /// <param name="result">The <see cref="QueryResult"/> to classify.</param>
    /// <returns>The outcome.</returns>
    internal static string For(QueryResult result) => Classify(result.IsAuthorized, result.HasExceptions, result.ValidationResults);

    static string Classify(bool isAuthorized, bool hasExceptions, IEnumerable<ValidationResult> validationResults)
    {
        if (!isAuthorized)
        {
            return WellKnownOperationOutcomes.Authorization;
        }

        if (hasExceptions)
        {
            return WellKnownOperationOutcomes.Error;
        }

        var results = validationResults as IReadOnlyCollection<ValidationResult> ?? [.. validationResults];
        if (results.Count == 0)
        {
            return WellKnownOperationOutcomes.Success;
        }

        return results.Any(IsAppendRejection) ? WellKnownOperationOutcomes.AppendRejected : WellKnownOperationOutcomes.Validation;
    }

    static bool IsAppendRejection(ValidationResult result) =>
        result.Reason == ValidationResultReason.ConstraintViolation ||
        result.Reason == ValidationResultReason.ConcurrencyViolation;
}
