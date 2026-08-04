// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries;

/// <summary>
/// Exception that gets thrown when a command requires a non-nullable read model that does not exist for the command's
/// resolved key.
/// </summary>
/// <remarks>
/// The command carried a usable key, but no read model exists for it. A nullable read model dependency tolerates this by
/// receiving null; a non-nullable ("must exist") dependency cannot, and this would otherwise surface as a server error
/// (HTTP 500) leaking the read model type. It implements <see cref="IValidationFailure"/>, so the pipeline surfaces it as
/// a validation failure (HTTP 400) with a message that does not reveal the read model type.
/// </remarks>
/// <param name="readModelType">The type of read model that does not exist.</param>
public class ReadModelDoesNotExistForCommand(Type readModelType)
    : Exception($"Read model of type '{readModelType.FullName}' does not exist for the command's resolved key."),
      IValidationFailure
{
    /// <summary>
    /// Gets the type of read model that does not exist.
    /// </summary>
    public Type ReadModelType { get; } = readModelType;

    /// <inheritdoc/>
    /// <remarks>
    /// Carries <see cref="ValidationResultReason.DependencyUnavailable"/> because no rule rejected anything - the
    /// command was never evaluated against its rules at all. Without that, this is indistinguishable from a domain
    /// rejection, and a spec asserting only that the command was rejected passes with every rule neutered.
    /// </remarks>
    public ValidationResult ValidationResult { get; } = ValidationResult.Error(
        "The command targets an entity that does not exist.",
        reason: ValidationResultReason.DependencyUnavailable);
}
