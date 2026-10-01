// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents how far along a slice is.
/// </summary>
/// <remarks>
/// A generated document describes an application that exists, so every slice it holds is
/// <see cref="Done"/>. The other values are here because the board reads them.
/// </remarks>
public enum SliceStatus
{
    /// <summary>
    /// Nothing has started on the slice.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// The slice is a draft.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// The slice is ready to be implemented.
    /// </summary>
    ReadyForImplementation = 2,

    /// <summary>
    /// The slice is being implemented.
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// The slice is ready for review.
    /// </summary>
    ReadyForReview = 4,

    /// <summary>
    /// The slice is done.
    /// </summary>
    Done = 5,

    /// <summary>
    /// The slice is being analyzed.
    /// </summary>
    Analysis = 6
}
