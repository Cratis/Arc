// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents what the model endpoint answers with - the document, and everything there is to say about it.
/// </summary>
/// <param name="EventModel">The document the board draws, or null when the source did not compile.</param>
/// <param name="Errors">What stopped the source from compiling, empty when it did.</param>
/// <param name="Warnings">What the board cannot hold, so a reader can see it is drawing less than the source says.</param>
/// <param name="Success">Whether a document was produced.</param>
public record EventModelView(
    EventModel? EventModel,
    IReadOnlyList<EventModelDiagnostic> Errors,
    IReadOnlyList<EventModelDiagnostic> Warnings,
    bool Success);
