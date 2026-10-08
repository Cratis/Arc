// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents the structure of a screen read from the Cratis Components it uses.
/// </summary>
/// <param name="Titles">The titles, in the order the components state them.</param>
/// <param name="Tables">The tables, in the order the components state them.</param>
/// <param name="Actions">The commands of the slice the screen runs, in the order the components state them.</param>
public record ScreenStructure(IEnumerable<string> Titles, IEnumerable<ScreenTableModel> Tables, IEnumerable<string> Actions)
{
    /// <summary>
    /// Gets the structure of a screen nothing was read from.
    /// </summary>
    public static readonly ScreenStructure None = new([], [], []);
}
