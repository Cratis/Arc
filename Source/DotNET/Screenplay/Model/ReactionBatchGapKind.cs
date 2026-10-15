// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Identifies the batch semantics not proven by source analysis.
/// </summary>
internal enum ReactionBatchGapKind
{
    /// <summary>
    /// The batch has rejecting constraints that must be represented and may require atomic appends.
    /// </summary>
    Rejection = 0,

    /// <summary>
    /// An earlier event closes the stream before another event in the batch.
    /// </summary>
    Closing = 1,

    /// <summary>
    /// A constraint's effects could not be read.
    /// </summary>
    Unreadable = 2,

    /// <summary>
    /// A rejecting constraint is not represented by the emitted document.
    /// </summary>
    Unrepresented = 3
}
