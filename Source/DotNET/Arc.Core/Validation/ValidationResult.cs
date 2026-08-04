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
/// <remarks>
/// <see cref="State"/> belongs to whoever authored the rule - it carries FluentValidation's <c>WithState</c> value
/// straight through - so it is not where the framework says what kind of rejection this is. That is
/// <see cref="Reason"/>.
/// </remarks>
public record ValidationResult(ValidationResultSeverity Severity, string Message, IEnumerable<string> Members, object State)
{
    /// <summary>
    /// Gets what composed this result. Defaults to <see cref="ValidationResultReason.Rule"/> - an authored rule
    /// rejected the input - so anything the framework composes on the application's behalf has to say so.
    /// </summary>
    public ValidationResultReason Reason { get; init; } = ValidationResultReason.Rule;

    /// <summary>
    /// Creates a new <see cref="ValidationResult"/> representing information.
    /// </summary>
    /// <param name="message">Message of the information.</param>
    /// <param name="members">Collection of member names that are related to the information.</param>
    /// <param name="state">State associated with the validation result.</param>
    /// <param name="reason">What composed the result. Defaults to <see cref="ValidationResultReason.Rule"/>.</param>
    /// <returns>A <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Information(string message, IEnumerable<string>? members = default, object? state = default, ValidationResultReason? reason = default)
        => new(ValidationResultSeverity.Information, message, members ?? [], state!) { Reason = reason ?? ValidationResultReason.Rule };

    /// <summary>
    /// Creates a new <see cref="ValidationResult"/> representing a warning.
    /// </summary>
    /// <param name="message">Message of the warning.</param>
    /// <param name="members">Collection of member names that caused the warning.</param>
    /// <param name="state">State associated with the validation result.</param>
    /// <param name="reason">What composed the result. Defaults to <see cref="ValidationResultReason.Rule"/>.</param>
    /// <returns>A <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Warning(string message, IEnumerable<string>? members = default, object? state = default, ValidationResultReason? reason = default)
        => new(ValidationResultSeverity.Warning, message, members ?? [], state!) { Reason = reason ?? ValidationResultReason.Rule };

    /// <summary>
    /// Creates a new <see cref="ValidationResult"/> representing an error.
    /// </summary>
    /// <param name="message">Message of the error.</param>
    /// <param name="members">Collection of member names that caused the error.</param>
    /// <param name="state">State associated with the validation result.</param>
    /// <param name="reason">What composed the result. Defaults to <see cref="ValidationResultReason.Rule"/>.</param>
    /// <returns>A <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Error(string message, IEnumerable<string>? members = default, object? state = default, ValidationResultReason? reason = default)
        => new(ValidationResultSeverity.Error, message, members ?? [], state!) { Reason = reason ?? ValidationResultReason.Rule };
}
