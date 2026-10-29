// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Declares the highest validation severity a caller may make non-blocking for this command.
/// Still subject to <see cref="BlockOnValidationSeverityAttribute"/> and the read-protection-On floor.
/// </summary>
/// <param name="maximum">The highest allowed caller severity.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class AllowCallerValidationSeverityAttribute(ValidationResultSeverity maximum) : Attribute
{
    /// <summary>
    /// Gets the highest allowed caller severity.
    /// </summary>
    public ValidationResultSeverity Maximum { get; } = maximum;
}
