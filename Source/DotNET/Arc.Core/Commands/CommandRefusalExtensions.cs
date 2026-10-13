// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Classifies command results into business refusals.
/// </summary>
public static class CommandRefusalExtensions
{
    /// <summary>
    /// Classifies a failed command without acknowledging exceptions, concurrency or unknown failure reasons.
    /// </summary>
    /// <param name="result">The command result to classify.</param>
    /// <returns>The refusal, or null for success or a non-refusal failure.</returns>
    /// <remarks>
    /// Exceptions and error-severity validation reasons other than Rule or ConstraintViolation prevent classification,
    /// even in mixed results. Warning and information results never prevent classification. Otherwise authorization wins over constraints, which win over error-severity rules.
    /// All error constraint names and messages are retained in result order, with the first name also available
    /// as ConstraintName. Messages are joined with a newline. Validation joins all error rule messages in result
    /// order. Authorization retains its reason verbatim; configuration failures are not denials.
    /// </remarks>
    public static CommandRefusal? GetRefusal(this CommandResult result)
    {
        var errors = result.ValidationResults.Where(validation => validation.Severity == ValidationResultSeverity.Error).ToArray();
        if (CannotClassify(result, errors))
        {
            return null;
        }

        if (!result.IsAuthorized)
        {
            return new(CommandRefusalKind.Authorization, null, result.AuthorizationFailureReason);
        }

        var constraints = errors.Where(validation => validation.Reason == ValidationResultReason.ConstraintViolation).ToArray();
        if (constraints.Length > 0)
        {
            return new(CommandRefusalKind.Constraint, constraints[0].ReasonDetail, JoinMessages(constraints))
            {
                ConstraintNames = constraints.Select(validation => validation.ReasonDetail).OfType<string>().Distinct().ToArray()
            };
        }

        return errors.Length > 0 ? new(CommandRefusalKind.Validation, null, JoinMessages(errors)) : null;
    }

    static bool CannotClassify(CommandResult result, IEnumerable<ValidationResult> validations) =>
        result.HasExceptions || result.AuthorizationConfigurationFailed || validations.Any(IsNonRefusal);

    static bool IsNonRefusal(ValidationResult validation) =>
        validation.Reason != ValidationResultReason.Rule && validation.Reason != ValidationResultReason.ConstraintViolation;

    static string JoinMessages(IEnumerable<ValidationResult> validations) =>
        string.Join('\n', validations.Select(validation => validation.Message));
}
