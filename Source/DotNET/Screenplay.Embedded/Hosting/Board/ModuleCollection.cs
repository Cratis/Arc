// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a collection of modules placed on the board.
/// </summary>
/// <param name="Id">The identity of the collection.</param>
/// <param name="Position">Where the collection sits on the board.</param>
/// <param name="Modules">The modules the collection holds.</param>
/// <param name="Actors">The actors declared for the collection - always empty, Screenplay has no board actors.</param>
/// <remarks>
/// Screenplay has no notion of a collection, so a document always lands in exactly one, positioned at origin.
/// Everything inside is laid out by the board itself from nesting and sort order.
/// </remarks>
public record ModuleCollection(
    Guid Id,
    Position Position,
    IReadOnlyList<Module> Modules,
    IReadOnlyList<Actor> Actors);
