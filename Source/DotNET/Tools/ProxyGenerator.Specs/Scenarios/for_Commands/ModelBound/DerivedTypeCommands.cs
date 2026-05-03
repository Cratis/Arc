// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

/// <summary>
/// A command with two properties using the same interface, each intended to receive a different derived type.
/// </summary>
[Command]
public class DerivedTypeCommand
{
    /// <summary>
    /// Gets or sets the first shape.
    /// </summary>
    public IShape? Shape1 { get; set; }

    /// <summary>
    /// Gets or sets the second shape.
    /// </summary>
    public IShape? Shape2 { get; set; }

    /// <summary>
    /// Handles the command and captures the concrete types and property values that were received.
    /// </summary>
    /// <returns>A <see cref="DerivedTypeCommandResult"/> with the resolved type names and shape values.</returns>
    public DerivedTypeCommandResult Handle()
    {
        var circle = Shape1 as CircleShape;
        var rect = Shape2 as RectangleShape;
        return new()
        {
            Shape1TypeName = Shape1?.GetType().Name ?? "null",
            Shape2TypeName = Shape2?.GetType().Name ?? "null",
            Shape1Radius = circle?.Radius ?? 0,
            Shape2Width = rect?.Width ?? 0,
            Shape2Height = rect?.Height ?? 0
        };
    }
}

/// <summary>
/// Result for <see cref="DerivedTypeCommand"/> capturing the concrete type names and property values of both shapes.
/// </summary>
public class DerivedTypeCommandResult
{
    /// <summary>
    /// Gets or sets the runtime type name of the first shape.
    /// </summary>
    public string Shape1TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the runtime type name of the second shape.
    /// </summary>
    public string Shape2TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the radius of the first shape (populated only when it is a <see cref="CircleShape"/>).
    /// </summary>
    public double Shape1Radius { get; set; }

    /// <summary>
    /// Gets or sets the width of the second shape (populated only when it is a <see cref="RectangleShape"/>).
    /// </summary>
    public double Shape2Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the second shape (populated only when it is a <see cref="RectangleShape"/>).
    /// </summary>
    public double Shape2Height { get; set; }
}
