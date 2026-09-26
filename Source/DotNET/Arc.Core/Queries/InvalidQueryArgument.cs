// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when a supplied scalar query argument cannot be converted to its declared type.
/// </summary>
/// <param name="argumentName">The name of the invalid argument.</param>
/// <param name="argumentType">The declared type of the argument.</param>
/// <param name="queryName">The query receiving the argument.</param>
public class InvalidQueryArgument(string argumentName, Type argumentType, FullyQualifiedQueryName queryName)
    : Exception($"Invalid argument '{argumentName}' of type '{argumentType.Name}' when performing query '{queryName}'"), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult { get; } = ValidationResult.Error(
        $"Invalid argument '{argumentName}' of type '{argumentType.Name}' when performing query '{queryName}'",
        [argumentName],
        reason: ValidationResultReason.MalformedRequest);
}
