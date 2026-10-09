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
/// written. What is recovered is what can be read without guessing: which file realizes it, which of the slice's
/// queries it binds, and the structure stated through the properties of known Cratis Components - the title of a
/// data page, the tables and columns of its data tables and the commands its command dialogs run. Structure written
/// in any other JSX is not read, because inventing it would put a confident falsehood into a document whose whole
/// value is that it is true.
/// </remarks>
public record ScreenModel(string Name, string FilePath)
{
    /// <summary>
    /// Gets the read models the screen binds through the queries of its slice.
    /// </summary>
    public IEnumerable<ScreenDataModel> Data { get; init; } = [];

    /// <summary>
    /// Gets the titles the screen shows, in the order its components state them.
    /// </summary>
    public IEnumerable<string> Titles { get; init; } = [];

    /// <summary>
    /// Gets the tables the screen shows, in the order its components state them.
    /// </summary>
    public IEnumerable<ScreenTableModel> Tables { get; init; } = [];

    /// <summary>
    /// Gets the commands of its slice the screen runs, in the order its components state them.
    /// </summary>
    public IEnumerable<string> Actions { get; init; } = [];
}
