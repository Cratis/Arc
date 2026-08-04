// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Strings;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Extension methods for converting constraint violations to validation results.
/// </summary>
public static class ConstraintViolationExtensions
{
    /// <summary>
    /// Converts a <see cref="ConstraintViolation"/> to a <see cref="ValidationResult"/> error, attributing the
    /// violation to the offending member when the violation details carry the property name — so a client can attach
    /// the message to the field it belongs to. The member is camel cased to match how command validation reports
    /// members.
    /// </summary>
    /// <param name="violation">The <see cref="ConstraintViolation"/> to convert.</param>
    /// <returns>A <see cref="ValidationResult"/> representing the violation.</returns>
    /// <remarks>
    /// The result carries <see cref="ValidationResultReason.ConstraintViolation"/>: the message is the constraint's,
    /// not an authored rule's, and a client that treats the two the same cannot tell a store-level rejection from
    /// one its own domain rules produced.
    /// </remarks>
    public static ValidationResult ToValidationResult(this ConstraintViolation violation)
    {
        string[] members = violation.Details is { } details && details.TryGetValue(WellKnownConstraintDetailKeys.PropertyName, out var propertyName)
            ? [propertyName.ToCamelCase()]
            : [];

        return ValidationResult.Error(violation.Message.Value, members, reason: ValidationResultReason.ConstraintViolation);
    }
}
