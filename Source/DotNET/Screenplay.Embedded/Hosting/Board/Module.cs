// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a module on the board.
/// </summary>
/// <param name="Id">The identity of the module.</param>
/// <param name="Name">The name of the module.</param>
/// <param name="Features">The features the module holds.</param>
/// <param name="Collapsed">Whether the module is drawn collapsed.</param>
/// <param name="SortOrder">Where the module sorts among its siblings.</param>
/// <param name="CommentCount">The number of comments on the module - always zero, a generated document carries no authoring state.</param>
public record Module(
    Guid Id,
    string Name,
    IReadOnlyList<Feature> Features,
    bool Collapsed,
    int SortOrder,
    int CommentCount);
