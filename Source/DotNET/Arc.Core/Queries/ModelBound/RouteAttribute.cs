// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Attribute to specify a custom route for a read model or query method.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RouteAttribute(string route) : Attribute
{
    /// <summary>
    /// Gets the custom route.
    /// </summary>
    public string Route { get; } = route;
}
