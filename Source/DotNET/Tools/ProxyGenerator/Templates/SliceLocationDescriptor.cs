// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Describes the slice location of a command or query for templating purposes.
/// </summary>
/// <param name="Module">The module the slice belongs to; empty when the namespace is too shallow.</param>
/// <param name="Feature">The feature the slice belongs to; empty when the namespace is too shallow.</param>
/// <param name="Slice">The slice itself; empty when the namespace has no segments left.</param>
public record SliceLocationDescriptor(string Module, string Feature, string Slice)
{
    /// <summary>
    /// Gets a slice location where every part is empty.
    /// </summary>
    public static readonly SliceLocationDescriptor Empty = new(string.Empty, string.Empty, string.Empty);
}
