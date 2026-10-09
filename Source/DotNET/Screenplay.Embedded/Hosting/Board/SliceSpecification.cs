// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a specification on a slice.
/// </summary>
/// <param name="Id">The identity of the specification.</param>
/// <param name="Name">The name of the specification, followed by what it states that the board has no card for.</param>
/// <param name="Given">The events that have happened before the specification acts.</param>
/// <param name="When">What the specification does - the command it runs, or the action it takes instead - when it states one.</param>
/// <param name="ThenEvents">The events the specification expects to be appended.</param>
/// <param name="ThenErrors">The rejections the specification expects.</param>
/// <param name="Collapsed">Whether the specification is drawn collapsed - always false, nothing on the board is folded away.</param>
/// <remarks>
/// The board draws given events, the action and the expected events and errors as cards. Everything else a
/// specification states - read models given or expected, the caller, the clock, generated values, the response
/// <c>then returns</c> expects, the read models expected to be absent - has no card on the board, so it travels in
/// the specification's name, the way Screenplay's own board shows it.
/// </remarks>
public record SliceSpecification(
    Guid Id,
    string Name,
    IReadOnlyList<SpecificationStep> Given,
    SpecificationAction? When,
    IReadOnlyList<SpecificationStep> ThenEvents,
    IReadOnlyList<SpecificationError> ThenErrors,
    bool Collapsed);
