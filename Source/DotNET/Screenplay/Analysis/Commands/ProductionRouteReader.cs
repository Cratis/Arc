// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Resolves the whole effective wrapper route using Arc's coupled definition and per-field sentinel fallback.
/// </summary>
/// <param name="models">The models resolving source definitions.</param>
/// <param name="types">The nominal type registry.</param>
internal class ProductionRouteReader(SemanticModels models, TypeRegistry types)
{
    /// <summary>
    /// Gets analyzed source definitions owning legacy stored classifications.
    /// </summary>
    internal IReadOnlyList<INamedTypeSymbol> Definitions { get; init; } = [];

    /// <summary>
    /// Reads the effective classified route of a returned wrapper.
    /// </summary>
    /// <param name="wrapper">The bound destination envelope.</param>
    /// <param name="command">The command supplying fallback metadata.</param>
    /// <param name="model">The wrapper's semantic model.</param>
    /// <param name="fallback">The proven command route.</param>
    /// <param name="identifierType">The explicit destination's nominal type.</param>
    /// <param name="sources">The registered generated sources.</param>
    /// <returns>The whole effective route, or null for an unproven route.</returns>
    internal CommandRouteModel? Read(
        BaseObjectCreationExpressionSyntax wrapper,
        INamedTypeSymbol command,
        SemanticModel model,
        CommandRouteModel? fallback,
        TypeReferenceModel? identifierType,
        AuthoringSources? sources)
    {
        var assignments = wrapper.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>().ToList() ?? [];
        if ((wrapper.Initializer is { } initializer && assignments.Count != initializer.Expressions.Count) ||
            assignments.Exists(assignment => model.GetSymbolInfo(assignment.Left).Symbol is not IPropertySymbol property ||
                !property.ContainingType.Is(EventWrapperType.MetadataName) ||
                property.Name is not ("EventSource" or "EventStream" or "EventSourceType" or "EventStreamType" or "EventStreamId")))
        {
            return null;
        }
        if (assignments.Select(assignment => ((IPropertySymbol)model.GetSymbolInfo(assignment.Left).Symbol!).Name).Distinct(StringComparer.Ordinal).Count() != assignments.Count)
        {
            return null;
        }
        var unreadableInheritedKey = fallback?.Stream is null &&
            (command.HasAttribute(WellKnownTypeNames.EventStreamIdAttribute) || command.FindInterface(WellKnownTypeNames.CanProvideEventStreamId) is not null);
        var members = assignments.ToDictionary(assignment => ((IPropertySymbol)model.GetSymbolInfo(assignment.Left).Symbol!).Name, assignment => assignment.Right, StringComparer.Ordinal);
        var source = Text("EventSourceType", "Default");
        var stream = Text("EventStreamType", "All");
        if ((members.ContainsKey("EventSourceType") && source is null) || (members.ContainsKey("EventStreamType") && stream is null))
        {
            return null;
        }
        CommandRouteModel? route;
        var definition = members.GetValueOrDefault("EventSource") is TypeOfExpressionSyntax typeOf ? model.GetTypeInfo(typeOf.Type).Type as INamedTypeSymbol : null;
        if (members.ContainsKey("EventSource") && definition is null)
        {
            return null;
        }
        if (definition is not null || (members.ContainsKey("EventStream") && source == "Default" && stream == "All"))
        {
            definition ??= command.GetAttribute(WellKnownTypeNames.ArcEventSourceAttributeOfT)?.AttributeClass?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
            if (definition is null || !members.TryGetValue("EventStream", out var selected) ||
                EventSourceReader.ReadDefinition(definition, selected, models, model) is not { } binding)
            {
                return null;
            }
            route = new(binding.Source, binding.Stream, identifierType, fallback?.StreamIdType, fallback?.StreamId)
            {
                StoredSource = binding.StoredSource,
                StoredStream = binding.StoredStream,
                StreamIdLiteral = fallback?.StreamIdLiteral
            };
        }
        else
        {
            if (members.ContainsKey("EventStream"))
            {
                // Legacy type strings clear definition routing, so a simultaneous definition stream is not a route proof.
                return null;
            }
            route = fallback is null ? null : fallback with { IdentifierType = identifierType };
            if (source != "Default" || stream != "All")
            {
                var effectiveSource = source == "Default" ? fallback?.Identity.StoredSource : source;
                var effectiveStream = stream == "All" ? fallback?.Identity.StoredStream : stream;
                if (effectiveSource is null || effectiveStream is null)
                {
                    return null;
                }
                var owners = Definitions.Where(candidate => candidate.FindInterface("Cratis.Chronicle.EventSources.IEventSource") is not null &&
                    EventSourceNames.StoredSource(candidate) == effectiveSource).ToList();
                if (owners.Count > 1 || (owners.Count == 1 && EventSourceReader.ReadStoredDefinition(owners[0], effectiveStream, models) is null))
                {
                    return null;
                }
                var binding = owners.Count == 1 ? EventSourceReader.ReadStoredDefinition(owners[0], effectiveStream, models) : null;
                route = new(binding?.Source ?? effectiveSource, binding?.Stream ?? effectiveStream, identifierType, fallback?.StreamIdType, fallback?.StreamId)
                {
                    StoredSource = binding?.StoredSource,
                    StoredStream = binding?.StoredStream,
                    StreamIdLiteral = fallback?.StreamIdLiteral
                };
            }
        }
        if (route is null || route.Stream is null || !ValidName(route.Source) || !ValidName(route.Stream) ||
            route.Identity.StoredSource == "Default" || route.Identity.StoredStream == "All" ||
            !EventSourceNames.Portable(route.Identity.StoredSource) || !EventSourceNames.Portable(route.Identity.StoredStream!))
        {
            return null;
        }
        if (members.TryGetValue("EventStreamId", out var key))
        {
            var literal = ProductionValues.Text(key, model);
            if (literal == "Default")
            {
                return unreadableInheritedKey ? null : route;
            }
            if (literal is not null)
            {
                return EventSourceNames.Portable(literal) ? route with { StreamIdType = new("String", false, false), StreamId = null, StreamIdLiteral = new(literal) } : null;
            }
            var path = ProductionValues.Path(key, model, command, sources);
            var property = command.DeclaredProperties().SingleOrDefault(property => property.Name == path);
            if (property is null)
            {
                return null;
            }

            return route with { StreamIdType = types.Resolve(property.Type), StreamId = path, StreamIdLiteral = null };
        }

        return unreadableInheritedKey ? null : route;

        string? Text(string name, string sentinel) => members.TryGetValue(name, out var value) ? ProductionValues.Text(value, model) : sentinel;
    }

    static bool ValidName(string name) => ScreenplayIdentifier.IsBareIdentifier(name);
}
