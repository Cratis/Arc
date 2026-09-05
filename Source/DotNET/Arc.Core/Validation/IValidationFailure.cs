// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Implemented by an exception that represents invalid command input rather than a server fault.
/// </summary>
/// <remarks>
/// When an exception implementing this interface surfaces within the command pipeline, it is converted into a command
/// validation failure (mapping to HTTP 400) instead of an error response (HTTP 500). Throw an exception implementing
/// this to reject a command as invalid from code that runs inside the pipeline — for example resolving a dependency
/// that requires client-provided input that was not supplied.
/// </remarks>
public interface IValidationFailure
{
    /// <summary>
    /// Gets the <see cref="ValidationResult"/> describing why the command is invalid.
    /// </summary>
    ValidationResult ValidationResult { get; }
}
