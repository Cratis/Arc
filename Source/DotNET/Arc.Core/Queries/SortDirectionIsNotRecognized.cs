// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when a query request carries a sort direction that is not one of the recognized
/// spellings.
/// </summary>
/// <remarks>
/// It implements <see cref="IValidationFailure"/> so the request is rejected as a client error (HTTP 400) carrying
/// <see cref="ValidationResultReason.MalformedRequest"/>, rather than being answered with results in an order the
/// caller did not ask for.
/// </remarks>
/// <param name="value">The unrecognized sort direction.</param>
/// <param name="member">The name of the request member the direction was read from.</param>
public class SortDirectionIsNotRecognized(string value, string member)
    : Exception($"Sort direction '{value}' is not recognized. Use 'asc', 'ascending', 'desc' or 'descending'."),
      IValidationFailure
{
    /// <summary>
    /// Gets the unrecognized sort direction.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Gets the name of the request member the direction was read from.
    /// </summary>
    public string Member { get; } = member;

    /// <inheritdoc/>
    public ValidationResult ValidationResult { get; } = ValidationResult.Error(
        "The sort direction is not a recognized value.",
        members: [member],
        reason: ValidationResultReason.MalformedRequest);
}
