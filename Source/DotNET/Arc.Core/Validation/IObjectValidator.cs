// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Defines a non-generic interface for validating an object instance without requiring generic type parameters at the call site.
/// </summary>
/// <remarks>
/// This interface enables AOT-safe validation by pushing generic instantiation into the concrete validator,
/// avoiding <c>MakeGenericType</c> + <c>Activator.CreateInstance</c> on <c>ValidationContext&lt;T&gt;</c>.
/// </remarks>
public interface IObjectValidator
{
    /// <summary>
    /// Validates the given object instance asynchronously.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="FluentValidation.Results.ValidationResult"/> describing any validation failures.</returns>
    Task<FluentValidation.Results.ValidationResult> ValidateObjectAsync(object instance, CancellationToken cancellationToken = default);
}
