// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Projections;
using Cratis.Arc.Screenplay.Model.Graphs;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Reads Chronicle's annotation-driven graph mutations, never fluent application code.</summary>
/// <param name="diagnostics">Where unsupported behavior is reported.</param>
public class GraphHandlerReader(ScreenplayDiagnostics diagnostics)
{
    readonly Dictionary<string, INamedTypeSymbol> _events = new(StringComparer.Ordinal);

    /// <summary>Gets every event referenced by the recovered graph handlers.</summary>
    public IEnumerable<INamedTypeSymbol> Events => _events.Values;

    /// <summary>Reads a node or edge's handlers.</summary>
    /// <param name="type">The graph element.</param>
    /// <returns>The handlers, or null when behavior cannot be recovered.</returns>
    public IReadOnlyList<GraphHandlerModel>? Read(INamedTypeSymbol type)
    {
        if (!GraphAnnotations.Supported(type))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"The graph element '{type.Name}' contains unsupported annotations or custom behavior; its graph is withheld", type.ContainingNamespace.ToDisplayString());
            return null;
        }

        var location = type.ContainingNamespace.ToDisplayString();
        var mappings = ModelBoundPropertyMappings.From(type, diagnostics, location);
        var handlers = type.GetAttributes().Where(GraphAnnotations.IsHandler).Select(attribute => ReadHandler(type, attribute, mappings)).ToList();
        if (type.HasAttribute(GraphNames.Node))
        {
            foreach (var eventType in PropertyEvents(type).Where(eventType => handlers.TrueForAll(handler => handler.Event != eventType.Name)))
            {
                _events.TryAdd(eventType.ToDisplayString(), eventType);
                handlers.Add(new(eventType.Name, eventType.ContainingNamespace.ToDisplayString(), false, false, ProjectionExpressions.EventSourceId, null, null, null, Mappings(type, eventType, mappings)));
            }
        }

        return [.. handlers.OrderBy(handler => handler.Event, StringComparer.Ordinal).ThenBy(handler => handler.Removes)];
    }

    static IEnumerable<INamedTypeSymbol> PropertyEvents(INamedTypeSymbol type) =>
        type.DeclaredProperties().SelectMany(MemberAttributes.Of).Where(GraphAnnotations.IsMapping).Select(attribute => attribute.AttributeClass?.TypeArguments.FirstOrDefault()).OfType<INamedTypeSymbol>();

    static string Selector(object? value) => value is string path ? ProjectionPaths.Convert(path) ?? path : ProjectionExpressions.EventSourceId;

    static Dictionary<string, string> Mappings(INamedTypeSymbol type, INamedTypeSymbol eventType, ModelBoundPropertyMappings mappings)
    {
        var result = new Dictionary<string, string>(mappings.For(eventType.Name), StringComparer.Ordinal);

        // Chronicle constants retain their CLR type. Do not guess whether a string such as "true" is a boolean.
        foreach (var property in type.DeclaredProperties())
        {
            foreach (var attribute in MemberAttributes.Of(property).Where(attribute => attribute.AttributeClass.Is(ModelBoundNames.SetValue) && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.TypeArguments.FirstOrDefault(), eventType)))
            {
                if (attribute.GetArgument(0) is string text)
                {
                    result[property.Name] = ProjectionExpressions.Enumeration(text);
                }
            }
        }

        return result;
    }

    GraphHandlerModel ReadHandler(INamedTypeSymbol type, AttributeData attribute, ModelBoundPropertyMappings mappings)
    {
        var eventType = (INamedTypeSymbol)attribute.AttributeClass!.TypeArguments[0];
        _events.TryAdd(eventType.ToDisplayString(), eventType);
        var edge = type.HasAttribute(GraphNames.Edge);
        var removes = attribute.AttributeClass.Is(ModelBoundNames.RemovedWith) || attribute.AttributeClass.Is(GraphNames.EdgeRemoved);
        var detach = attribute.GetArgument(0) is true || attribute.GetNamedArgument("Detach") is true;
        var key = attribute.GetNamedArgument("ConstantKey") is string constant ? ProjectionExpressions.Enumeration(constant) : Selector(attribute.GetArgument(0));
        return new(
            eventType.Name,
            eventType.ContainingNamespace.ToDisplayString(),
            removes,
            detach,
            edge ? null : key,
            edge ? Selector(attribute.GetArgument(0)) : null,
            edge ? Selector(attribute.GetArgument(1)) : null,
            edge && attribute.GetArgument(2) is string identity ? Selector(identity) : null,
            removes ? new Dictionary<string, string>(StringComparer.Ordinal) : Mappings(type, eventType, mappings));
    }
}
