// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// A command property needed to select a stream is blank.
/// </summary>
/// <param name="model">The model to read.</param>
/// <param name="property">The missing command property.</param>
public class ReadModelStreamPartMissing(Type model, string property) : Exception($"'{property}' is required to read {model.Name}"), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult { get; } = ValidationResult.Error($"'{property}' is required to read {model.Name}", [property]);
}
