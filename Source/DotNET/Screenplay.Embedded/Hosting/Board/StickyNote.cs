// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a sticky note on the board.
/// </summary>
/// <param name="Id">The identity of the note.</param>
/// <param name="Position">Where the note sits on the board.</param>
/// <param name="Width">The width of the note.</param>
/// <param name="Height">The height of the note.</param>
/// <param name="Text">The text on the note.</param>
/// <remarks>
/// A generated document never holds one - a sticky note is authoring state, and nothing in a Screenplay
/// document says where one would go.
/// </remarks>
public record StickyNote(string Id, Position Position, double Width, double Height, string Text);
