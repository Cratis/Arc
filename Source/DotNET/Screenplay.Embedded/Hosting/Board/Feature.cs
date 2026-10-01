// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a feature - and, through its sub-features, a feature tree - on the board.
/// </summary>
/// <param name="Id">The identity of the feature.</param>
/// <param name="Name">The name of the feature.</param>
/// <param name="SubFeatures">The features nested inside this one.</param>
/// <param name="Slices">The slices the feature holds.</param>
/// <param name="Collapsed">Whether the feature is drawn collapsed.</param>
/// <param name="RowCollapsed">Whether the feature's row is drawn collapsed.</param>
/// <param name="Enabled">Whether the feature is enabled.</param>
/// <param name="CommentCount">The number of comments on the feature - always zero, a generated document carries no authoring state.</param>
public record Feature(
    Guid Id,
    string Name,
    IReadOnlyList<Feature> SubFeatures,
    IReadOnlyList<Slice> Slices,
    bool Collapsed,
    bool RowCollapsed,
    bool Enabled,
    int CommentCount);
