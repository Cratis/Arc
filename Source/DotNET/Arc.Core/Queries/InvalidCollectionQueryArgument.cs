// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when a collection query argument contains an invalid element or has a nested collection shape.
/// </summary>
/// <param name="targetType">The declared collection type.</param>
/// <param name="value">The invalid value.</param>
public class InvalidCollectionQueryArgument(Type targetType, object? value)
    : Exception($"Invalid collection argument of type '{targetType.Name}': '{value}'."), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult { get; } = ValidationResult.Error(
        $"Invalid collection argument of type '{targetType.Name}'.",
        members: ["arguments"],
        reason: ValidationResultReason.MalformedRequest);
}
