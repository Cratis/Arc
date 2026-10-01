// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a position on the board.
/// </summary>
/// <param name="X">The horizontal position.</param>
/// <param name="Y">The vertical position.</param>
public record Position(double X, double Y)
{
    /// <summary>
    /// Gets the position everything a generated document places sits at.
    /// </summary>
    public static readonly Position Origin = new(0, 0);
}
