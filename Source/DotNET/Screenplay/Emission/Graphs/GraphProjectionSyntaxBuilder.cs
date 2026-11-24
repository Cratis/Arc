// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Projections;
using Cratis.Arc.Screenplay.Model.Graphs;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Graphs;

/// <summary>Builds a single canonical graph projection without claiming to recover Stage metadata.</summary>
/// <param name="naming">The naming policy.</param>
/// <param name="diagnostics">Where incomplete behavior is reported.</param>
public class GraphProjectionSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    /// <summary>Builds the Arc fallback projection, named for its placement slice.</summary>
    /// <param name="graph">The recovered graph.</param>
    /// <param name="sliceName">The placement slice's emitted name.</param>
    /// <returns>The projection, or null when behavior cannot be represented completely.</returns>
    public GraphProjectionSyntax? Build(GraphModel graph, string sliceName)
    {
        var elements = graph.Elements.Where(element => element.Handlers.Count > 0).Select(element => BuildElement(graph, element)).ToArray();
        if (elements.Length == 0)
        {
            return null;
        }

        if (elements.Any(element => element is null))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"The graph '{graph.Name}' contains an unexpressible handler; its projection is withheld", graph.Namespace);
            return null;
        }

        return new(sliceName, graph.Name, elements.OfType<ProjectionBlockSyntax>(), SourceLocation.Start);
    }

    static bool TryValue(string? expression, out ExpressionSyntax? value)
    {
        value = null;
        return expression is null || ProjectionExpressionConverter.TryConvert(expression, out value);
    }

    ProjectionBlockSyntax? BuildElement(GraphModel graph, GraphElementModel element)
    {
        var handlers = element.Handlers.Select(handler => BuildHandler(graph, element, handler)).ToArray();
        if (handlers.Any(handler => handler is null))
        {
            return null;
        }

        return element.Source is null
            ? new GraphNodeBlockSyntax(element.Name, handlers.OfType<ProjectionBlockSyntax>(), SourceLocation.Start)
            : new GraphEdgeBlockSyntax(element.Name, handlers.OfType<ProjectionBlockSyntax>(), SourceLocation.Start);
    }

    ProjectionBlockSyntax? BuildHandler(GraphModel graph, GraphElementModel element, GraphHandlerModel handler)
    {
        var mappings = new ProjectionMappingConverter(naming, diagnostics, graph.Namespace, element.Name, new NameAvailability(naming, diagnostics))
            .Convert(handler.Mappings, ReservedWords.None).ToArray();
        if (mappings.Length != handler.Mappings.Count)
        {
            return null;
        }

        return element.Source is null ? BuildNodeHandler(handler, mappings) : BuildEdgeHandler(handler, mappings);
    }

    ProjectionBlockSyntax? BuildNodeHandler(GraphHandlerModel handler, MappingSyntax[] mappings)
    {
        if (!TryValue(handler.Key, out var key))
        {
            return null;
        }

        return handler.Removes
            ? new GraphRemoveSyntax(naming.ToDeclarationName(handler.Event), key, null, null, handler.Detach, SourceLocation.Start)
            : new GraphFromSyntax(naming.ToDeclarationName(handler.Event), key, null, null, mappings, SourceLocation.Start);
    }

    ProjectionBlockSyntax? BuildEdgeHandler(GraphHandlerModel handler, MappingSyntax[] mappings)
    {
        if (!TryValue(handler.Source, out var source))
        {
            return null;
        }

        if (!TryValue(handler.Target, out var target))
        {
            return null;
        }

        if (!TryValue(handler.Identity, out var identity))
        {
            return null;
        }

        return handler.Removes
            ? new GraphRemoveSyntax(naming.ToDeclarationName(handler.Event), null, source, target, handler.Detach, SourceLocation.Start) { Identity = identity }
            : new GraphFromSyntax(naming.ToDeclarationName(handler.Event), null, source, target, mappings, SourceLocation.Start) { Identity = identity };
    }
}
