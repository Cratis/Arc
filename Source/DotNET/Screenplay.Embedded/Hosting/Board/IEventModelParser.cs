// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Defines a parser that turns embedded Screenplay source into the document the board draws.
/// </summary>
public interface IEventModelParser
{
    /// <summary>
    /// Parses Screenplay source into a board document.
    /// </summary>
    /// <param name="documentId">The identifier of the document - every identity in the result is derived from it.</param>
    /// <param name="name">The name to give the document when the source names no domain.</param>
    /// <param name="source">The Screenplay source.</param>
    /// <returns>The resulting <see cref="EventModelView"/>.</returns>
    EventModelView Parse(string documentId, string name, string source);
}
