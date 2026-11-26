// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Orders the sibling slices of a feature so that a slice declaring an event is written before the slices whose
/// projections consume it.
/// </summary>
/// <remarks>
/// A timeline reads a document top to bottom, so a projection reading an event no earlier slice declared binds
/// against nothing. Slices that depend on nothing keep their ordinal order, and slices in a cycle keep it among
/// themselves, so every slice is always kept and the result is deterministic. An event declared by more than one
/// sibling, or by none, adds no ordering, and a slice consuming its own event adds none either.
/// </remarks>
public static class SliceDependencyOrder
{
    /// <summary>
    /// Orders the slices of a feature.
    /// </summary>
    /// <param name="slices">The sibling slices.</param>
    /// <returns>The slices in dependency order, ties broken by ordinal slice name.</returns>
    public static IReadOnlyList<SliceSyntax> Apply(IEnumerable<SliceSyntax> slices)
    {
        var ordinal = slices.OrderBy(slice => slice.Name, StringComparer.Ordinal).ToList();
        var owners = ordinal
            .SelectMany((slice, index) => slice.Events.Select(@event => (@event.Name, index)))
            .GroupBy(declared => declared.Name, StringComparer.Ordinal)
            .Where(group => group.Select(_ => _.index).Distinct().Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.Ordinal);
        var before = ordinal
            .Select((slice, index) => ConsumedEvents(slice).Where(owners.ContainsKey).Select(name => owners[name]).Where(owner => owner != index).ToHashSet())
            .ToList();

        var placed = new List<int>();
        var remaining = Enumerable.Range(0, ordinal.Count).ToList();
        while (remaining.Count > 0)
        {
            var ready = remaining.FirstOrDefault(index => before[index].All(placed.Contains), -1);
            var next = ready >= 0 ? ready : CycleMember(remaining, before);
            placed.Add(next);
            remaining.Remove(next);
        }

        return [.. placed.Select(index => ordinal[index])];
    }

    static int CycleMember(IReadOnlyList<int> remaining, List<HashSet<int>> before)
    {
        return remaining.First(IsCyclic);

        bool IsCyclic(int start)
        {
            var pending = new Stack<int>(before[start].Where(remaining.Contains));
            var visited = new HashSet<int>();
            while (pending.TryPop(out var dependency))
            {
                if (dependency == start)
                {
                    return true;
                }

                if (visited.Add(dependency))
                {
                    foreach (var next in before[dependency].Where(remaining.Contains))
                    {
                        pending.Push(next);
                    }
                }
            }

            return false;
        }
    }

    static IEnumerable<string> ConsumedEvents(SliceSyntax slice) =>
        slice.Projections.SelectMany(projection => BlockEvents(projection.Blocks));

    static IEnumerable<string> BlockEvents(IEnumerable<ProjectionBlockSyntax> blocks) =>
        blocks.SelectMany(block => block switch
        {
            FromSyntax from => from.Events.Select(_ => _.Event),
            JoinSyntax join => join.Events.Select(_ => _.Event),
            ChildrenSyntax children => BlockEvents(children.Blocks),
            NestedSyntax nested => BlockEvents(nested.Blocks),
            ClearWithSyntax clear => [clear.Event],
            RemoveWithSyntax remove => [remove.Event],
            RemoveViaJoinSyntax removeViaJoin => [removeViaJoin.Event],
            ProjectionVariantSyntax variant => variant.EntersOn.Select(_ => _.Event).Concat(BlockEvents(variant.Blocks)),
            _ => []
        });
}
