// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected command read has an invalid key or a required model is missing.
/// </summary>
public sealed class ProtectedReadModelDoesNotExist : Exception, IValidationFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProtectedReadModelDoesNotExist"/> class.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <param name="key">The original key text, intentionally excluded from both messages.</param>
    public ProtectedReadModelDoesNotExist(Type model, string key)
        : base($"{model.Name} does not exist.")
    {
        _ = key;
        ValidationResult = ValidationResult.Error(Message);
    }

    /// <inheritdoc/>
    public ValidationResult ValidationResult { get; }
}
