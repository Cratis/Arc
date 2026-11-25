// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Model.Graphs;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Discovers graphs and supplies deterministic Arc placement when annotations retain none.</summary>
/// <param name="registry">The type registry shared with the application.</param>
/// <param name="events">The reader for graph-only event references.</param>
/// <param name="diagnostics">Where incomplete recovery is reported.</param>
public class GraphCatalog(TypeRegistry registry, EventReader events, ScreenplayDiagnostics diagnostics)
{
    readonly SortedSet<string> _imports = new(StringComparer.Ordinal);

    /// <summary>Gets graph-only dependencies declared by referenced assemblies.</summary>
    public IEnumerable<string> Imports => _imports;

    /// <summary>Reads graphs and ensures their placement and referenced events exist in the slice tree.</summary>
    /// <param name="types">The source types.</param>
    /// <param name="slices">The application slices, extended for graph-only declarations and events.</param>
    /// <returns>The supported graphs.</returns>
    public IReadOnlyList<GraphModel> Read(IEnumerable<INamedTypeSymbol> types, IList<SliceModel> slices)
    {
        var declared = types.ToArray();
        foreach (var fluent in declared.Where(type => type.AllInterfaces.Any(contract => contract.FullMetadataName() == $"{GraphNames.Namespace}.IGraphProjection")))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"The fluent graph projection '{fluent.Name}' is code; it cannot be recovered as an annotated graph", fluent.Namespace());
        }

        var groups = declared.Where(type => type.HasAttribute(GraphNames.Node) || type.HasAttribute(GraphNames.Edge))
            .GroupBy(GraphShapeReader.GraphOf).OrderBy(group => group.Key, StringComparer.Ordinal).ToArray();
        var declarations = new GraphProjectionDeclarationReader(declared, diagnostics);
        declarations.ReportUnknownGraphs(groups.Select(group => group.Key));
        return [.. groups.Select(group => ReadGraph(group, slices, declarations)).OfType<GraphModel>()];
    }

    static bool InvalidEvent(INamedTypeSymbol type) => type.TypeKind == TypeKind.Error || !EventReader.IsEvent(type);

    static SliceModel EnsureSlice(IList<SliceModel> slices, string location)
    {
        var existing = slices.FirstOrDefault(slice => slice.Namespace == location);
        if (existing is not null)
        {
            return existing;
        }

        var created = new SliceModel(location, location.Split('.')[^1], SliceKind.StateView, null, [], [], [], [], [], []);
        slices.Add(created);
        return created;
    }

    GraphModel? ReadGraph(IGrouping<string, INamedTypeSymbol> group, IList<SliceModel> slices, GraphProjectionDeclarationReader declarations)
    {
        var handlers = new GraphHandlerReader(diagnostics);
        var records = group.OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal).ToArray();
        var elements = ReadElements(records, handlers);
        if (elements.Any(element => element is null) || !GraphTopology.Valid(group, elements.OfType<GraphElementModel>()))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"The graph '{group.Key}' has ambiguous elements or endpoints; it is withheld", group.First().Namespace());
            return null;
        }

        var referenced = handlers.Events.ToArray();
        if (!RegisterEvents(referenced, slices))
        {
            return null;
        }

        var owners = referenced.Where(type => type.DeclaringSyntaxReferences.Length > 0).Select(type => type.Namespace())
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var placement = owners.FirstOrDefault() ?? group.Select(type => type.Namespace()).Order(StringComparer.Ordinal).First();
        var explicitDeclarations = declarations.HasDeclarations(group.Key);
        IEqualityComparer<ISymbol> symbolComparer = SymbolEqualityComparer.Default;
        var projections = explicitDeclarations
            ? declarations.Read(group.Key, records.Zip(elements).ToDictionary(pair => pair.First, pair => pair.Second!, symbolComparer))
            : [];
        if (!explicitDeclarations)
        {
            ReportPlacement(group.Key, placement, owners, referenced.Length);
        }

        EnsureSlice(slices, placement);
        foreach (var projection in projections)
        {
            EnsureSlice(slices, projection.Namespace);
        }

        return new(group.Key, placement, [.. elements.OfType<GraphElementModel>()])
        {
            HasProjectionPlacement = owners.Length > 0,
            HasProjectionDeclarations = explicitDeclarations,
            Projections = projections
        };
    }

    GraphElementModel?[] ReadElements(IEnumerable<INamedTypeSymbol> records, GraphHandlerReader handlers)
    {
        var shapes = new GraphShapeReader(registry, diagnostics);
        return records.Select(type => handlers.Read(type) is { } behavior ? shapes.Read(type, behavior) : null).ToArray();
    }

    void ReportPlacement(string graph, string placement, string[] owners, int eventCount)
    {
        if (owners.Length > 1)
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.CanonicalGraphPlacement, $"The graph '{graph}' observes events in several slices. Arc places its single canonical projection in '{placement}'; this is a fallback, not recovered Stage ownership", placement);
        }

        if (owners.Length == 0 && eventCount > 0)
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"The graph '{graph}' has no source-declared handler event slice; its shape is retained but its projection is withheld rather than fabricating placement", placement);
        }
    }

    bool RegisterEvents(INamedTypeSymbol[] referenced, IList<SliceModel> slices)
    {
        if (referenced.Any(InvalidEvent) ||
            referenced.GroupBy(type => type.Name, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, "A graph references unresolved, unregistered or ambiguously named events; its projection is withheld");
            return false;
        }

        foreach (var type in referenced)
        {
            if (type.DeclaringSyntaxReferences.Length == 0)
            {
                _imports.Add(type.ToDisplayString());
                continue;
            }

            var slice = EnsureSlice(slices, type.Namespace());
            if (slice.Events.All(existing => existing.Name != type.Name))
            {
                slices[slices.IndexOf(slice)] = slice with { Events = [.. slice.Events, events.Read(type, type.Namespace())] };
            }
        }

        return true;
    }
}
