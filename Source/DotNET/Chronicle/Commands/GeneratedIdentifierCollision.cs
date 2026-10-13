// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a generated identifier targets an occupied event source during aggregate acquisition.
/// </summary>
public class GeneratedIdentifierCollision() : Exception("The generated identifier targets an event source that already has events."), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult => CreateValidationResult();

    /// <summary>
    /// Creates a redacted concurrency rejection with a stable collision detail.
    /// </summary>
    /// <param name="state">Optional violation state.</param>
    /// <returns>The collision validation result.</returns>
    internal static ValidationResult CreateValidationResult(object? state = null) => ValidationResult.Error(
        "The generated identifier targets an event source that already has events. Submit a new invocation to generate a new identifier.",
        state: state,
        reason: ValidationResultReason.ConcurrencyViolation,
        reasonDetail: nameof(GeneratedIdentifierCollision));
}
