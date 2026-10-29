// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Options for caller-supplied validation severity on model-bound command endpoints.
/// </summary>
public class CommandOptions
{
    /// <summary>
    /// Gets or sets the highest validation severity a caller may make non-blocking by default.
    /// </summary>
    public ValidationResultSeverity MaximumCallerAllowedSeverity { get; set; } = ValidationResultSeverity.Warning;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore caller-supplied allowed severity, including command opt-ins.
    /// </summary>
    public bool IgnoreCallerAllowedSeverity { get; set; }
}
