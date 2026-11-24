// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Projections;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Model.Graphs;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Reads the shape of annotated graph elements without executing application code.</summary>
/// <param name="types">The registry admitting graph-only concepts.</param>
/// <param name="diagnostics">Where unsupported shapes are reported.</param>
public class GraphShapeReader(TypeRegistry types, ScreenplayDiagnostics diagnostics)
{
    /// <summary>Reads the graph an element belongs to, following Chronicle's annotation precedence.</summary>
    /// <param name="type">The element.</param>
    /// <returns>The explicit name or the assembly's root namespace fallback.</returns>
    public static string GraphOf(INamedTypeSymbol type) =>
        type.GetAttribute(GraphNames.Graph)?.GetArgument(0) as string ??
        type.ContainingAssembly.GetAttribute(GraphNames.Graph)?.GetArgument(0) as string ??
        type.ContainingAssembly.Name;

    /// <summary>Reads one node or edge.</summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="handlers">The recovered handlers.</param>
    /// <returns>The element, or null when its shape cannot be stated.</returns>
    public GraphElementModel? Read(INamedTypeSymbol type, IReadOnlyList<GraphHandlerModel> handlers)
    {
        var edge = type.GetAttribute(GraphNames.Edge);
        var name = edge?.GetArgument(0) as string ?? type.Name;
        var properties = type.DeclaredProperties().ToArray();
        var source = Endpoint(properties, GraphNames.Source);
        var target = Endpoint(properties, GraphNames.Target);
        var carried = properties.Where(property => !MemberAttributes.Has(property, GraphNames.Source) && !MemberAttributes.Has(property, GraphNames.Target)).ToArray();
        var keys = carried.Where(property => MemberAttributes.Has(property, ModelBoundNames.Key)).ToArray();

        if (!ValidAnnotations(type, name, edge) || !ValidKeys(keys, edge, source, target) || !ValidProperties(carried))
        {
            return Unsupported(type);
        }

        return new(
            name,
            source?.Name,
            target?.Name,
            edge is null ? null : keys.FirstOrDefault()?.Name,
            [.. carried.Select(property => new GraphPropertyModel(new PropertyModel(property.Name, types.ResolveCarried(property.Type)), edge is null && MemberAttributes.Has(property, ModelBoundNames.Key)))],
            handlers);
    }

    /// <summary>Determines whether a name can be emitted unchanged as an identifier.</summary>
    /// <param name="name">The name.</param>
    /// <returns>Whether it is an identifier.</returns>
    internal static bool ValidName(string name) =>
        name.Length > 0 && (char.IsAsciiLetter(name[0]) || name[0] == '_') && name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    static bool ValidAnnotations(INamedTypeSymbol type, string name, AttributeData? edge) =>
        ValidName(name) && ValidName(GraphOf(type)) && type.HasAttribute(GraphNames.Node) != (edge is not null);

    static bool ValidKeys(IPropertySymbol[] keys, AttributeData? edge, INamedTypeSymbol? source, INamedTypeSymbol? target) =>
        edge is null ? keys.Length == 1 : source is not null && target is not null && keys.Length <= 1;

    static INamedTypeSymbol? Endpoint(IEnumerable<IPropertySymbol> properties, string annotation)
    {
        var attributes = properties.SelectMany(property => MemberAttributes.Of(property, annotation)).ToArray();
        return attributes.Length == 1 && attributes[0].AttributeClass?.TypeArguments.FirstOrDefault() is INamedTypeSymbol node && node.HasAttribute(GraphNames.Node)
            ? node
            : null;
    }

    bool ValidProperties(IEnumerable<IPropertySymbol> properties) =>
        properties.All(property => ValidName(property.Name) && types.WouldResolveTo(UnderlyingTypes.Of(property.Type)));

    GraphElementModel? Unsupported(INamedTypeSymbol type)
    {
        diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"The graph element '{type.ToDisplayString()}' has an unsupported shape, key or endpoint; its graph is withheld", type.ContainingNamespace.ToDisplayString());
        return null;
    }
}
