// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Represents a child collection or nested object a model-bound read model declares with attributes.
/// </summary>
/// <param name="Property">The read model property holding the children or nested object.</param>
/// <param name="Type">The type each instance is of.</param>
/// <param name="IdentifiedBy">The expression identifying each child, empty for a nested object.</param>
/// <param name="AutoMap">How automatic property mapping applies within the scope.</param>
/// <param name="From">The blocks the declaring attributes state themselves, which create the instances.</param>
public record ModelBoundChild(
    string Property,
    INamedTypeSymbol Type,
    string IdentifiedBy,
    ProjectionAutoMapMode AutoMap,
    IEnumerable<ProjectionFromModel> From)
{
    /// <summary>
    /// Builds the scope of the child, folding what the declaring attributes state into what its type declares.
    /// </summary>
    /// <param name="inner">Everything the type of the child declares itself.</param>
    /// <returns>The <see cref="ProjectionChildScopeModel"/>.</returns>
    /// <remarks>
    /// The attribute on the parent says which event brings an instance into being and how it is keyed, while the
    /// type of the instance says what that event fills in. Both describe the same block, so the two are merged
    /// rather than emitted as a block that keys nothing and a block that maps nothing.
    /// </remarks>
    public ProjectionChildScopeModel ToScope(ProjectionScopeModel inner)
    {
        var blocks = new Dictionary<string, ProjectionFromModel>(StringComparer.Ordinal);

        foreach (var block in inner.From)
        {
            blocks[EventNameOf(block)] = block;
        }

        foreach (var declared in From)
        {
            var name = EventNameOf(declared);
            blocks[name] = blocks.TryGetValue(name, out var existing)
                ? existing with { Key = declared.Key, ParentKey = declared.ParentKey }
                : declared;
        }

        return new(Property, IdentifiedBy, AutoMap, inner with
        {
            From = [.. blocks.Values.OrderBy(EventNameOf, StringComparer.Ordinal)]
        });
    }

    /// <summary>
    /// Gets the name of the event a block observes.
    /// </summary>
    /// <param name="from">The block to read.</param>
    /// <returns>The name, empty when the block names none.</returns>
    static string EventNameOf(ProjectionFromModel from) => from.EventTypes.FirstOrDefault() ?? string.Empty;
}
