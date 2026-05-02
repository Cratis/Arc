// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Serialization;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Interface for shapes used in derived type testing.
/// </summary>
public interface IShape
{
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    string Label { get; set; }
}

/// <summary>
/// A circle shape — carries a derived type identifier so the proxy generator emits <c>@derivedType</c>.
/// </summary>
[DerivedType("4a5b6c7d-0000-0000-0000-000000000001")]
public class CircleShape : IShape
{
    /// <inheritdoc/>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the radius.
    /// </summary>
    public double Radius { get; set; }
}

/// <summary>
/// A rectangle shape — carries a derived type identifier so the proxy generator emits <c>@derivedType</c>.
/// </summary>
[DerivedType("4a5b6c7d-0000-0000-0000-000000000002")]
public class RectangleShape : IShape
{
    /// <inheritdoc/>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public double Height { get; set; }
}

/// <summary>
/// A read model whose <see cref="Shape"/> property uses an interface that has concrete derived types.
/// </summary>
[ReadModel]
public class DerivedTypeReadModel
{
    /// <summary>
    /// Gets or sets the shape.
    /// </summary>
    public IShape? Shape { get; set; }

    /// <summary>
    /// Gets a read model containing a circle shape.
    /// </summary>
    /// <returns>A <see cref="DerivedTypeReadModel"/> with a circle shape.</returns>
    public static DerivedTypeReadModel GetWithCircleShape() => new()
    {
        Shape = new CircleShape { Label = "Circle", Radius = 5.0 }
    };

    /// <summary>
    /// Gets a read model containing a rectangle shape.
    /// </summary>
    /// <returns>A <see cref="DerivedTypeReadModel"/> with a rectangle shape.</returns>
    public static DerivedTypeReadModel GetWithRectangleShape() => new()
    {
        Shape = new RectangleShape { Label = "Rectangle", Width = 10.0, Height = 20.0 }
    };
}
