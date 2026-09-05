// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Defines a system that validates an object graph by running every discoverable validator that applies to it.
/// </summary>
/// <remarks>
/// Whether a value is well formed is a property of its type, not of the operation carrying it — a
/// <see cref="ConceptValidator{T}"/> for an identifier means the same thing on a command as it does on a query
/// argument. This is the single traversal both pipelines use, so a validator behaves identically wherever the value
/// it guards appears.
/// </remarks>
public interface IModelGraphValidator
{
    /// <summary>
    /// Validates an object graph, running validators for the root and for everything reachable from it.
    /// </summary>
    /// <param name="request">The <see cref="ModelGraphValidationRequest"/> describing what to validate.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for cancelling the validation.</param>
    /// <returns>The <see cref="ValidationResult"/> collection describing every failure found, empty when valid.</returns>
    Task<IEnumerable<ValidationResult>> Validate(ModelGraphValidationRequest request, CancellationToken cancellationToken = default);
}
