// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a screen of a slice, realized by a file and bound to the queries it reads.
/// </summary>
/// <param name="Name">The name of the screen, as the file realizing it is named.</param>
/// <param name="FilePath">The path of the file realizing the screen, relative to the root of the source.</param>
/// <remarks>
/// A screen is the one part of a slice whose realization is not C#, so most of what it shows stays where it is
/// written. Two things are still recovered: which file realizes it, which is what lets a reader open it, and which
/// of the slice's queries it binds, which is a name the model already holds and can be checked against. What the
/// screen then does with that data - its sections, tables, columns and actions - is structure expressed in JSX, and
/// inventing it would put a confident falsehood into a document whose whole value is that it is true.
/// </remarks>
public record ScreenModel(string Name, string FilePath)
{
    /// <summary>
    /// Gets the read models the screen binds through the queries of its slice.
    /// </summary>
    public IEnumerable<ScreenDataModel> Data { get; init; } = [];
}
