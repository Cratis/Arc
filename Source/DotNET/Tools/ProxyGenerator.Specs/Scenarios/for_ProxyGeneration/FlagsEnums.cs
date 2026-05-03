// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// Represents the edges of a UI element to which it is anchored.
/// Multiple edges can be combined using bitwise operations.
/// </summary>
[Flags]
public enum AnchorEdges
{
    /// <summary>
    /// No anchoring is set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Anchor to the top edge.
    /// </summary>
    Top = 1 << 0,

    /// <summary>
    /// Anchor to the right edge.
    /// </summary>
    Right = 1 << 1,

    /// <summary>
    /// Anchor to the bottom edge.
    /// </summary>
    Bottom = 1 << 2,

    /// <summary>
    /// Anchor to the left edge.
    /// </summary>
    Left = 1 << 3,
}
