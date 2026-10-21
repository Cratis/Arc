// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected command read has an invalid key or a required model is missing.
/// </summary>
/// <param name="model">The read model type.</param>
/// <param name="key">The original key text, or an empty string for a composite key.</param>
public sealed class ProtectedReadModelDoesNotExist(Type model, string key)
    : Exception($"{model.Name} '{key}' does not exist."), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult { get; } = ValidationResult.Error($"{model.Name} '{key}' does not exist.");
}
