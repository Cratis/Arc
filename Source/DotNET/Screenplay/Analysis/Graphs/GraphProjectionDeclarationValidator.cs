// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model.Graphs;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Validates ownership as a set so malformed metadata never silently drops behavior.</summary>
/// <param name="graph">The graph being declared.</param>
/// <param name="elements">The catalog's canonical graph record symbols and their recovered models.</param>
/// <param name="diagnostics">Where invalid metadata is reported.</param>
public class GraphProjectionDeclarationValidator(string graph, IReadOnlyDictionary<ISymbol, GraphElementModel> elements, ScreenplayDiagnostics diagnostics)
{
    readonly Dictionary<(AssemblyIdentity Assembly, string Name), GraphElementModel> _records =
        elements.ToDictionary(pair => Identity(pair.Key), pair => pair.Value);
    readonly Dictionary<(AssemblyIdentity Assembly, string Name), INamedTypeSymbol> _owners = [];
    readonly List<GraphProjectionModel> _projections = [];
    readonly List<(string Location, string Message)> _errors = [];

    /// <summary>Validates one marker and records its owned elements.</summary>
    /// <param name="marker">The marker type.</param>
    /// <param name="attribute">The source metadata.</param>
    /// <param name="name">The exact projection name.</param>
    public void Add(INamedTypeSymbol marker, AttributeData attribute, string name)
    {
        if (!GraphShapeReader.ValidName(name) || string.IsNullOrEmpty(marker.Namespace()))
        {
            Invalid(marker, name, "projection name must be an identifier and the marker must declare a slice namespace");
        }

        if (attribute.ConstructorArguments.Length != 3 || attribute.ConstructorArguments[2].IsNull)
        {
            Invalid(marker, "<members>", "a nonempty node/edge member list is required");
            return;
        }

        var members = attribute.ConstructorArguments[2].Values;
        if (members.Length == 0)
        {
            Invalid(marker, "<members>", "a nonempty node/edge member list is required");
        }

        var owned = members.OrderBy(member => member.Value?.ToString(), StringComparer.Ordinal)
            .Select(member => ReadMember(marker, member)).OfType<GraphElementModel>()
            .OrderBy(element => element.Name, StringComparer.Ordinal).ToArray();
        RequireHandlers(marker, name, owned);
        _projections.Add(new(name, marker.Namespace(), owned));
    }

    /// <summary>Checks coverage and withholds all explicit projections on any invalid ownership.</summary>
    /// <returns>The valid projection set, sorted by namespace and name.</returns>
    public IReadOnlyList<GraphProjectionModel> Complete()
    {
        foreach (var missing in elements.Where(pair => pair.Value.Handlers.Count > 0 && !_owners.ContainsKey(Identity(pair.Key))))
        {
            _errors.Add((missing.Key.ContainingNamespace.ToDisplayString(), $"Graph '{graph}', member '{missing.Key.ToDisplayString()}': handler-bearing record has no declaration owner; assign it to a projection marker"));
        }

        foreach (var error in _errors.OrderBy(error => error.Location, StringComparer.Ordinal).ThenBy(error => error.Message, StringComparer.Ordinal))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration, error.Message, error.Location);
        }

        return _errors.Count > 0 ? [] : [.. _projections.OrderBy(projection => projection.Namespace, StringComparer.Ordinal).ThenBy(projection => projection.Name, StringComparer.Ordinal)];
    }

    static (AssemblyIdentity Assembly, string Name) Identity(ISymbol type) => (type.ContainingAssembly.Identity, MetadataName(type));

    static string MetadataName(ISymbol type)
    {
        if (type.ContainingType is { } declaring)
        {
            return $"{MetadataName(declaring)}+{type.MetadataName}";
        }

        return type.ContainingNamespace.IsGlobalNamespace ? type.MetadataName : $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";
    }

    GraphElementModel? ReadMember(INamedTypeSymbol marker, TypedConstant member)
    {
        if (member.Value is not INamedTypeSymbol type || type.TypeKind == TypeKind.Error)
        {
            Invalid(marker, member.Value?.ToString() ?? "<unresolved>", "member type is unresolved");
            return null;
        }

        var identity = Identity(type);
        if (!_records.TryGetValue(identity, out var element))
        {
            Invalid(marker, type.ToDisplayString(), "member must be an annotated node or edge of this graph");
            return null;
        }

        if (!_owners.TryAdd(identity, marker))
        {
            Invalid(marker, type.ToDisplayString(), $"member is already owned by marker '{_owners[identity].ToDisplayString()}' (including duplicate entries)");
            return null;
        }

        return element;
    }

    void RequireHandlers(INamedTypeSymbol marker, string name, GraphElementModel[] owned)
    {
        if (owned.Length > 0 && !owned.Any(element => element.Handlers.Count > 0))
        {
            Invalid(marker, name, "declaration owns no handler-bearing node or edge");
        }
    }

    void Invalid(INamedTypeSymbol marker, string member, string reason) =>
        _errors.Add((marker.Namespace(), $"Graph '{graph}', marker '{marker.ToDisplayString()}', member '{member}': {reason}"));
}
