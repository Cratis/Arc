// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// A command mixing required and optional properties, for verifying that the generated proxy keeps the nullability of
/// every property.
/// </summary>
public class CommandWithOptionalProperties
{
    /// <summary>
    /// Gets or sets the required name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the optional value.
    /// </summary>
    public int? Value { get; set; }

    /// <summary>
    /// Gets or sets the required tags.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional labels.
    /// </summary>
    public IEnumerable<string>? Labels { get; set; }
}
