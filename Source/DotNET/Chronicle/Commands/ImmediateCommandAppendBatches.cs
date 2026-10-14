// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Plans immediate fallback appends without splitting a generated source's freshness guard or changing event order.
/// </summary>
internal static class ImmediateCommandAppendBatches
{
    /// <summary>
    /// Materializes the entire fallback plan before any append can take effect.
    /// </summary>
    /// <param name="targets">The ordered returned events.</param>
    /// <param name="freshness">The invocation's freshness guards.</param>
    /// <param name="sequence">The destination sequence.</param>
    /// <returns>Atomic generated-source batches and individual unguarded events, in original order.</returns>
    /// <exception cref="GeneratedIdentifierEventsCannotBeSplit">Guarded events are interleaved with other sources.</exception>
    internal static IReadOnlyList<CommandAppendTarget[]> Create(List<CommandAppendTarget> targets, FreshIdentifierScopes freshness, EventSequenceId sequence)
    {
        ValidateOrder(targets, freshness, sequence);
        var batches = new List<CommandAppendTarget[]>();
        for (var index = 0; index < targets.Count;)
        {
            var source = targets[index].EventSourceId;
            var count = freshness.HasGuard(sequence, source)
                ? targets.Skip(index).TakeWhile(target => target.EventSourceId == source).Count()
                : 1;
            batches.Add(targets.GetRange(index, count).ToArray());
            index += count;
        }

        return batches;
    }

    static void ValidateOrder(IEnumerable<CommandAppendTarget> targets, FreshIdentifierScopes freshness, EventSequenceId sequence)
    {
        var guarded = targets.Select((target, index) => (target.EventSourceId, Index: index))
            .Where(target => freshness.HasGuard(sequence, target.EventSourceId))
            .GroupBy(target => target.EventSourceId);
        if (guarded.Any(group => group.Last().Index - group.First().Index + 1 != group.Count()))
        {
            throw new GeneratedIdentifierEventsCannotBeSplit();
        }
    }
}
