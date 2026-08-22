// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the wire-level bounds for observable query subscription revisions.
/// </summary>
public static class ObservableQuerySubscriptionRevision
{
    /// <summary>
    /// The largest revision that can be represented exactly by a JavaScript <c>number</c>.
    /// </summary>
    public const long MaxValue = 9_007_199_254_740_991;

    /// <summary>
    /// Determines whether a revision is valid on the wire.
    /// </summary>
    /// <param name="revision">The optional revision.</param>
    /// <returns><see langword="true"/> when missing for a legacy message, or when positive and within the safe integer range.</returns>
    public static bool IsValid(long? revision) =>
        revision is null || (revision > 0 && revision <= MaxValue);
}
