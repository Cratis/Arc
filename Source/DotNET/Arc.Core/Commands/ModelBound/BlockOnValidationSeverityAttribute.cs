// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Declares the minimum validation severity that blocks a model-bound command. The caller may require a stricter threshold, but cannot loosen this one.
/// Unknown-severity failures also block a command with this attribute.
/// </summary>
/// <param name="severity">The lowest severity that blocks the command.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BlockOnValidationSeverityAttribute(ValidationResultSeverity severity) : Attribute
{
    /// <summary>
    /// Gets the lowest severity that blocks the command.
    /// </summary>
    public ValidationResultSeverity Severity { get; } = severity;
}
