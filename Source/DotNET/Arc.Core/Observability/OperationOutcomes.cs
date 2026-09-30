// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Observability;

/// <summary>
/// The outcomes a command or query is reported with, and how a result maps to one.
/// </summary>
internal static class OperationOutcomes
{
    /// <summary>The operation succeeded.</summary>
    internal const string Success = "success";

    /// <summary>Validation rejected the operation.</summary>
    internal const string Validation = "validation";

    /// <summary>Authorization denied the operation.</summary>
    internal const string Authorization = "authorization";

    /// <summary>The event store rejected the append the command produced, through a constraint or a concurrency conflict.</summary>
    internal const string AppendRejected = "append_rejected";

    /// <summary>The operation failed with an error.</summary>
    internal const string Error = "error";

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
            return Authorization;
        }

        if (hasExceptions)
        {
            return Error;
        }

        var results = validationResults as IReadOnlyCollection<ValidationResult> ?? [.. validationResults];
        if (results.Count == 0)
        {
            return Success;
        }

        return results.Any(IsAppendRejection) ? AppendRejected : Validation;
    }

    static bool IsAppendRejection(ValidationResult result) =>
        result.Reason == ValidationResultReason.ConstraintViolation ||
        result.Reason == ValidationResultReason.ConcurrencyViolation;
}
