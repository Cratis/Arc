// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model.Graphs;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Checks that emitted endpoints resolve within exactly one graph.</summary>
public static class GraphTopology
{
    /// <summary>Checks element-name uniqueness and endpoint membership.</summary>
    /// <param name="types">The graph's annotated types.</param>
    /// <param name="elements">The recovered elements.</param>
    /// <returns>Whether the graph can be stated without ambiguous references.</returns>
    public static bool Valid(IEnumerable<INamedTypeSymbol> types, IEnumerable<GraphElementModel> elements)
    {
        var nodes = types.Where(type => type.HasAttribute(GraphNames.Node)).ToArray();
        var endpoints = types.SelectMany(type => type.DeclaredProperties()).SelectMany(MemberAttributes.Of)
            .Where(attribute => attribute.AttributeClass.Is(GraphNames.Source) || attribute.AttributeClass.Is(GraphNames.Target))
            .Select(attribute => attribute.AttributeClass!.TypeArguments[0]);
        return elements.GroupBy(element => element.Name, StringComparer.Ordinal).All(group => group.Count() == 1) &&
            endpoints.All(endpoint => nodes.Any(node => SymbolEqualityComparer.Default.Equals(node, endpoint)));
    }
}
