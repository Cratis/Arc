// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents the an failed validation rule.
/// </summary>
/// <param name="Severity">The <see cref="ValidationResultSeverity"/> of the result.</param>
/// <param name="Message">Message of the error.</param>
/// <param name="Members">Collection of member names that caused the failure.</param>
/// <param name="State">State associated with the validation result.</param>
public record ValidationResult(ValidationResultSeverity Severity, string Message, IEnumerable<string> Members, object State)
{
    /// <summary>
    /// Creates a new <see cref="ValidationResult"/> representing a warning.
    /// </summary>
    /// <param name="message">Message of the warning.</param>
    /// <param name="members">Collection of member names that caused the warning.</param>
    /// <param name="state">State associated with the validation result.</param>
    /// <returns>A <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Warning(string message, IEnumerable<string>? members = default, object? state = default)
        => new(ValidationResultSeverity.Warning, message, members ?? [], state!);

    /// <summary>
    /// Creates a new <see cref="ValidationResult"/> representing an error.
    /// </summary>
    /// <param name="message">Message of the error.</param>
    /// <param name="members">Collection of member names that caused the error.</param>
    /// <param name="state">State associated with the validation result.</param>
    /// <returns>A <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Error(string message, IEnumerable<string>? members = default, object? state = default)
        => new(ValidationResultSeverity.Error, message, members ?? [], state!);
}
