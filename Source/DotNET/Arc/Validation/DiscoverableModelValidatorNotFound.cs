// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// The exception that is thrown when a previously discovered validator is no longer available during MVC model validation.
/// </summary>
/// <param name="modelType">The model type whose validator could not be found.</param>
public class DiscoverableModelValidatorNotFound(Type modelType)
    : Exception($"The discoverable validator for '{modelType.FullName}' is no longer available during MVC model validation. Validation cannot proceed.");
