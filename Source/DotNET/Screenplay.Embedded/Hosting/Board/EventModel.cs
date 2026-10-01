// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents an event model document, shaped as the public <c>@cratis/event-models</c> package reads it.
/// </summary>
/// <param name="Id">The identity of the document.</param>
/// <param name="Name">The name of the document.</param>
/// <param name="Collections">The module collections the document holds.</param>
/// <param name="StickyNotes">The sticky notes on the board - always empty, a generated document carries no authoring state.</param>
/// <param name="Links">The links between module collections - always empty, a generated document holds a single collection.</param>
public record EventModel(
    Guid Id,
    string Name,
    IReadOnlyList<ModuleCollection> Collections,
    IReadOnlyList<StickyNote> StickyNotes,
    IReadOnlyList<ModuleCollectionLink> Links);
