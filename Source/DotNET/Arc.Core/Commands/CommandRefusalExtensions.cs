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
    /// Exceptions and validation reasons other than Rule or ConstraintViolation prevent classification, even in
    /// mixed results. Otherwise authorization wins over constraints, which win over error-severity rules.
    /// The first error constraint supplies the name; messages for that name are joined in result order with
    /// a newline. Validation joins all error rule messages in result order. Authorization retains its reason verbatim.
    /// </remarks>
    public static CommandRefusal? GetRefusal(this CommandResult result)
    {
        var validations = result.ValidationResults.ToArray();
        if (result.HasExceptions || validations.Any(IsNonRefusal))
        {
            return null;
        }

        if (!result.IsAuthorized)
        {
            return new(CommandRefusalKind.Authorization, null, result.AuthorizationFailureReason);
        }

        var errors = validations.Where(validation => validation.Severity == ValidationResultSeverity.Error).ToArray();
        var constraint = errors.FirstOrDefault(validation => validation.Reason == ValidationResultReason.ConstraintViolation);
        if (constraint is not null)
        {
            var messages = errors.Where(validation => validation.Reason == ValidationResultReason.ConstraintViolation && validation.ReasonDetail == constraint.ReasonDetail);

            return new(CommandRefusalKind.Constraint, constraint.ReasonDetail, JoinMessages(messages));
        }

        return errors.Length > 0 ? new(CommandRefusalKind.Validation, null, JoinMessages(errors)) : null;
    }

    static bool IsNonRefusal(ValidationResult validation) =>
        validation.Reason != ValidationResultReason.Rule && validation.Reason != ValidationResultReason.ConstraintViolation;

    static string JoinMessages(IEnumerable<ValidationResult> validations) =>
        string.Join('\n', validations.Select(validation => validation.Message));
}
