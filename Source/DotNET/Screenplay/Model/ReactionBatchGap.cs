// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents why a returned batch cannot be stated using legacy per-fact appends.
/// </summary>
/// <param name="Kind">The missing batch capability.</param>
/// <param name="Message">The source-specific reason.</param>
internal record ReactionBatchGap(ReactionBatchGapKind Kind, string Message)
{
    /// <summary>
    /// Determines whether the available append semantics cannot represent this batch.
    /// </summary>
    /// <param name="atomic">Whether atomic reaction batches are admitted.</param>
    /// <returns>True when the batch must remain code.</returns>
    internal bool Withheld(bool atomic) => !atomic || Kind != ReactionBatchGapKind.Rejection;
}
