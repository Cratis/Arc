// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Validates an object through a validator's statically typed validation path.
/// </summary>
public interface IObjectValidator
{
    /// <summary>
    /// Validates the object using its concrete model type.
    /// </summary>
    /// <param name="instance">The model to validate.</param>
    /// <param name="cancellationToken">Token for cancelling validation.</param>
    /// <returns>The FluentValidation result.</returns>
    Task<FluentValidation.Results.ValidationResult> ValidateObjectAsync(object instance, CancellationToken cancellationToken = default);
}
