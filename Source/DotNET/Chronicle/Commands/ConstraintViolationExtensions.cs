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
    public static ValidationResult ToValidationResult(this ConstraintViolation violation)
    {
        string[] members = violation.Details.TryGetValue(WellKnownConstraintDetailKeys.PropertyName, out var propertyName)
            ? [propertyName.ToCamelCase()]
            : [];

        return ValidationResult.Error(violation.Message.Value, members);
    }
}
