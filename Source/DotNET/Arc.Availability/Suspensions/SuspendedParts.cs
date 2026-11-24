// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// The parts that are suspended for one subject.
/// </summary>
/// <param name="paths">The paths whose newest decision, at some level, is a suspension.</param>
public class SuspendedParts(IReadOnlyCollection<string> paths)
{
    /// <summary>
    /// Gets a value that suspends nothing.
    /// </summary>
    public static SuspendedParts None { get; } = new([]);

    /// <summary>
    /// Folds decisions into what is suspended: the newest decision wins per level and path, and a suspension at any level wins.
    /// </summary>
    /// <param name="decisions">The decisions for one subject.</param>
    /// <returns>The <see cref="SuspendedParts"/>.</returns>
    public static SuspendedParts From(IEnumerable<PartSuspensionDecision> decisions)
    {
        var newest = new Dictionary<(AudienceLevel Level, string Path), PartSuspensionDecision>();
        foreach (var decision in decisions)
        {
            var key = (decision.Level, decision.Path);
            if (!newest.TryGetValue(key, out var current) || decision.DecidedAt >= current.DecidedAt)
            {
                newest[key] = decision;
            }
        }

        return new([.. newest.Values.Where(_ => _.IsSuspended).Select(_ => _.Path).Distinct()]);
    }

    /// <summary>
    /// Determines whether a part is suspended, directly or through a module, feature or slice that contains it.
    /// </summary>
    /// <param name="part">The part.</param>
    /// <returns>True when suspended.</returns>
    public bool Covers(Type part) => paths.Any(_ => PartPaths.Covers(_, part));
}
