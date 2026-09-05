// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Attribute to specify a custom path for a read model or query method.
/// </summary>
/// <param name="path">The custom path.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PathAttribute(string path) : Attribute
{
    /// <summary>
    /// Gets the custom path.
    /// </summary>
    public string Path { get; } = path;
}
