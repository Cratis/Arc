// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model.Graphs;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Graphs;

/// <summary>Builds typed V38 graph declarations.</summary>
/// <param name="naming">The naming policy.</param>
public class GraphSyntaxBuilder(IScreenplayNaming naming)
{
    /// <summary>Builds a graph's shape.</summary>
    /// <param name="graph">The recovered graph.</param>
    /// <returns>The typed declaration.</returns>
    public GraphSyntax Build(GraphModel graph) => new(
        graph.Name,
        graph.Elements.Where(element => element.Source is null).Select(element => new GraphNodeSyntax(element.Name, Properties(element), SourceLocation.Start)),
        graph.Elements.Where(element => element.Source is not null).Select(element => new GraphEdgeSyntax(element.Name, element.Source!, element.Target!, Properties(element), SourceLocation.Start) { IdentifiedBy = element.Identity is null ? null : naming.ToPropertyName(element.Identity) }),
        SourceLocation.Start);

    IEnumerable<PropertySyntax> Properties(GraphElementModel element) => element.Properties.Select(property =>
        new PropertySyntax(naming.ToPropertyName(property.Property.Name), new TypeReferenceConverter(naming).Convert(property.Property.Type), SourceLocation.Start) { IsKey = property.IsKey });
}
