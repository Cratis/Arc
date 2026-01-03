// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Provides mapping between FluentValidation Severity and Arc ValidationResultSeverity.
/// </summary>
public static class SeverityMapper
{
    /// <summary>
    /// Maps Arc's <see cref="ValidationResultSeverity"/> to FluentValidation's <see cref="Severity"/>.
    /// </summary>
    /// <param name="severity">The <see cref="ValidationResultSeverity"/> to map.</param>
    /// <returns>The corresponding FluentValidation <see cref="Severity"/>.</returns>
    public static Severity ToFluentValidationSeverity(this ValidationResultSeverity severity) => severity switch
    {
        ValidationResultSeverity.Information => Severity.Info,
        ValidationResultSeverity.Warning => Severity.Warning,
        ValidationResultSeverity.Error => Severity.Error,
        _ => Severity.Error
    };

    /// <summary>
    /// Maps FluentValidation's <see cref="Severity"/> to Arc's <see cref="ValidationResultSeverity"/>.
    /// </summary>
    /// <param name="severity">The FluentValidation <see cref="Severity"/> to map.</param>
    /// <returns>The corresponding <see cref="ValidationResultSeverity"/>.</returns>
    public static ValidationResultSeverity ToValidationResultSeverity(this Severity severity) => severity switch
    {
        Severity.Info => ValidationResultSeverity.Information,
        Severity.Warning => ValidationResultSeverity.Warning,
        Severity.Error => ValidationResultSeverity.Error,
        _ => ValidationResultSeverity.Error
    };
}
