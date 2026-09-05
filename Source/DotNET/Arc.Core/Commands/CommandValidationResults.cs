// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Helpers for deciding which validation results on a <see cref="CommandResult"/> block command execution
/// relative to an allowed severity level.
/// </summary>
static class CommandValidationResults
{
    /// <summary>
    /// Gets the validation results that block execution for the given allowed severity.
    /// </summary>
    /// <param name="results">The validation results to filter.</param>
    /// <param name="allowedSeverity">The maximum allowed severity; results with higher severity block.</param>
    /// <returns>The blocking validation results.</returns>
    /// <remarks>
    /// When <paramref name="allowedSeverity"/> is <see langword="null"/>, only errors block execution.
    /// Otherwise, results with a severity higher than <paramref name="allowedSeverity"/> block execution.
    /// </remarks>
    public static IEnumerable<ValidationResult> Blocking(IEnumerable<ValidationResult> results, ValidationResultSeverity? allowedSeverity) =>
        allowedSeverity is null
            ? results.Where(_ => _.Severity == ValidationResultSeverity.Error)
            : results.Where(_ => _.Severity > allowedSeverity);

    /// <summary>
    /// Determines whether the given <see cref="CommandResult"/> blocks execution for the allowed severity.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to evaluate.</param>
    /// <param name="allowedSeverity">The maximum allowed validation severity.</param>
    /// <returns>True if the result should stop command execution; otherwise, false.</returns>
    public static bool IsBlocking(CommandResult result, ValidationResultSeverity? allowedSeverity) =>
        !result.IsAuthorized ||
        result.HasExceptions ||
        Blocking(result.ValidationResults, allowedSeverity).Any();
}
