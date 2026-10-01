// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Finds the events a slice reads without declaring them.
/// </summary>
/// <remarks>
/// A State View builds its read model from events other slices produce, and a reaction triggers on them.
/// Without them the slice arrives on the board with nothing flowing into it, so they are carried as events
/// referencing the item the producing slice holds.
/// </remarks>
internal static class ConsumedEvents
{
    /// <summary>
    /// Gets the names of the events a slice reads, in the order they are declared.
    /// </summary>
    /// <param name="slice">The slice to read.</param>
    /// <returns>The names of the events it reads.</returns>
    public static IEnumerable<string> ConsumedEventNames(this SliceSyntax slice) =>
        (slice.Projections ?? [])
            .SelectMany(projection => FromBlocks(projection.Blocks))
            .Concat((slice.Reactions ?? []).SelectMany(FromReaction))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    static IEnumerable<string> FromReaction(ReactionSyntax reaction) =>
        (reaction.Triggers ?? [])
            .Select(trigger => trigger.Source)
            .OfType<NamedTriggerSourceSyntax>()
            .Select(source => source.Name);

    static IEnumerable<string> FromBlocks(IEnumerable<ProjectionBlockSyntax>? blocks) =>
        (blocks ?? []).SelectMany(FromBlock);

    static IEnumerable<string> FromBlock(ProjectionBlockSyntax block) => block switch
    {
        FromSyntax from => (from.Events ?? []).Select(@event => @event.Event),
        JoinSyntax join => (join.Events ?? []).Select(@event => @event.Event),
        RemoveWithSyntax removeWith => [removeWith.Event],
        RemoveViaJoinSyntax removeViaJoin => [removeViaJoin.Event],
        ClearWithSyntax clearWith => [clearWith.Event],
        ChildrenSyntax children => FromBlocks(children.Blocks),
        NestedSyntax nested => FromBlocks(nested.Blocks),
        _ => []
    };
}
