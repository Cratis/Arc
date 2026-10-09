// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a slice on the board.
/// </summary>
/// <param name="Id">The identity of the slice.</param>
/// <param name="Name">The name of the slice.</param>
/// <param name="SliceType">The kind of slice it is.</param>
/// <param name="Description">The description the Screenplay document states for the slice.</param>
/// <param name="Status">The status of the slice - always <see cref="SliceStatus.Done"/>, the document describes an application that exists.</param>
/// <param name="Collapsed">Whether the slice is drawn collapsed.</param>
/// <param name="SortOrder">Where the slice sorts among its siblings.</param>
/// <param name="Command">The command the slice handles, when it has one.</param>
/// <param name="ReadModel">The read model the slice builds, when it has one.</param>
/// <param name="ExternalEvents">Events coming from outside the application - always empty, Screenplay declares every event it uses.</param>
/// <param name="Events">The events the slice produces, and the ones it consumes from other slices.</param>
/// <param name="Queries">The queries the slice answers.</param>
/// <param name="Actors">The user experience actors on the slice - always empty, a generated document holds no prototype.</param>
/// <param name="Specifications">The specifications on the slice, in the order the document declares them.</param>
/// <param name="CommentCount">The number of comments on the slice - always zero, a generated document carries no authoring state.</param>
public record Slice(
    Guid Id,
    string Name,
    SliceType SliceType,
    string Description,
    SliceStatus Status,
    bool Collapsed,
    int SortOrder,
    CommandItem? Command,
    ReadModelItem? ReadModel,
    IReadOnlyList<ExternalEventItem> ExternalEvents,
    IReadOnlyList<EventItem> Events,
    IReadOnlyList<QueryItem> Queries,
    IReadOnlyList<UserExperienceActor> Actors,
    IReadOnlyList<SliceSpecification> Specifications,
    int CommentCount);
