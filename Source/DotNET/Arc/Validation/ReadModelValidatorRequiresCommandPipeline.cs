// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// The exception that is thrown when a validator that depends on a command-scoped read model is used through the MVC
/// controller model-validation path, which runs before a command context is established.
/// </summary>
/// <param name="modelType">The command/model type whose validator could not be constructed.</param>
/// <param name="innerException">The underlying exception that occurred while constructing the validator.</param>
public class ReadModelValidatorRequiresCommandPipeline(Type modelType, Exception innerException)
    : Exception(
        $"The validator for '{modelType.FullName}' could not be constructed during MVC model validation. " +
        "Validators that take a read model as a dependency require the Arc command pipeline (minimal-API command endpoints or ICommandPipeline directly), " +
        "because the read model is resolved for the command's event source id from the command scope, which does not exist during MVC model binding. " +
        "Expose the command through a minimal-API endpoint, or move the read-model based check into the command's Handle() method.",
        innerException);
