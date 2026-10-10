// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Adds a named tag to every event returned by a command.
/// </summary>
/// <param name="name">The tag name.</param>
/// <param name="property">The command property supplying the value; omit when setting <see cref="Value"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class EventTagAttribute(string name, string? property = null) : Attribute
{
    /// <summary>
    /// Gets the tag name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the command property supplying the tag value.
    /// </summary>
    public string? Property { get; } = property;

    /// <summary>
    /// Gets or sets a constant tag value instead of a property.
    /// </summary>
    public string? Value { get; set; }
}
