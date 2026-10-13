// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Recovers identity, observation and independent append metadata without reading handler bodies.
/// </summary>
/// <param name="definitions">The application's registered definition types.</param>
/// <param name="models">The semantic models resolving attribute expressions.</param>
internal class ReactorMetadataReader(IEnumerable<INamedTypeSymbol> definitions, SemanticModels models)
{
    /// <summary>
    /// Reads the nearest declaration, replacing rather than combining inherited attributes.
    /// </summary>
    /// <param name="type">The reactor type.</param>
    /// <param name="attribute">The attribute metadata name.</param>
    /// <returns>The nearest attribute, or null.</returns>
    internal static AttributeData? Nearest(INamedTypeSymbol type, string attribute)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetAttribute(attribute) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether a scenario needs an explicit routed observation fixture.
    /// </summary>
    /// <param name="type">The reactor type.</param>
    /// <returns>Whether observation is narrower than the whole event log.</returns>
    internal static bool HasObservationFilter(INamedTypeSymbol type) =>
        type.HasAttribute(WellKnownTypeNames.FromEventSourceAttributeOfT) ||
        (Nearest(type, WellKnownTypeNames.EventSourceTypeAttribute) is { } source && source.GetArgument(0) is not "") ||
        (Nearest(type, WellKnownTypeNames.EventStreamTypeAttribute) is { } stream && stream.GetArgument(0) is not "All");

    /// <summary>
    /// Checks append routing independently of observation.
    /// </summary>
    /// <param name="type">The reactor type.</param>
    /// <param name="handlers">The reactor's handlers.</param>
    /// <param name="models">The semantic models used to resolve returned results.</param>
    /// <returns>Whether returned events require an unsupported append route.</returns>
    internal static bool RoutesAppends(INamedTypeSymbol type, IReadOnlyList<IMethodSymbol> handlers, SemanticModels models) =>
        new ReactorAppendRouting(models).UsesContextRoute(handlers) && HasAppendRoute(type);

    /// <summary>
    /// Adds metadata even when every handler remains code.
    /// </summary>
    /// <param name="type">The reactor type.</param>
    /// <param name="reactor">The body model.</param>
    /// <returns>The reactor with recovered metadata and explicit refusal evidence.</returns>
    internal ReactorModel Read(INamedTypeSymbol type, ReactorModel reactor)
    {
        var identity = ReadIdentity(type);
        var known = DefinitionsFor(type);
        var observation = ReadObservation(type, known);

        return reactor with
        {
            EventSource = observation.Filter,
            SystemRoles = identity.Roles,
            IdentityFailure = identity.Failure,
            ObservationFailure = observation.Failure,
            ObservationDefinitions = known,
            MetadataLocation = type.ToDisplayString()
        };
    }

    static bool HasAppendRoute(INamedTypeSymbol type) =>
        type.FindInterface(WellKnownTypeNames.CanProvideEventSourceId) is not null ||
        type.FindInterface(WellKnownTypeNames.CanProvideEventStreamId) is not null ||
        (Nearest(type, WellKnownTypeNames.EventSourceTypeAttribute) is { } source && source.GetArgument(0) is not "Default") ||
        (Nearest(type, WellKnownTypeNames.EventStreamTypeAttribute) is { } stream && stream.GetArgument(0) is not "All") ||
        (Nearest(type, WellKnownTypeNames.EventStreamIdAttribute) is { } id && id.GetArgument(0) is not "" and not "Default");

    static RecoveredIdentity ReadIdentity(INamedTypeSymbol type)
    {
        var attribute = Nearest(type, WellKnownTypeNames.ExecuteCommandsAsSystemAttribute);
        var roles = RolesOf(attribute);
        var failure = attribute?.AttributeClass is null ? null : IdentityFailure(roles);

        return new(roles, failure);
    }

    static string? IdentityFailure(List<string>? roles)
    {
        const string Failure = "system roles must be non-null, non-blank, distinct text; no roles were sanitized";
        if (roles is null)
        {
            return Failure;
        }
        if (roles.Exists(string.IsNullOrWhiteSpace))
        {
            return Failure;
        }

        return roles.Distinct(StringComparer.Ordinal).Count() != roles.Count ? Failure : null;
    }

    static ObservedEventSourceModel NormalizeTypedObservation(ObservedEventSourceModel observed, string? storedStream)
    {
        if (!observed.StreamDeclared)
        {
            return observed;
        }

        return string.Equals(storedStream, "All", StringComparison.Ordinal) ? observed with { Stream = null } : observed;
    }

    static RecoveredObservation ReadRawObservation(INamedTypeSymbol type, List<ReactorSourceDefinition> known)
    {
        var source = AttributeText(Nearest(type, WellKnownTypeNames.EventSourceTypeAttribute), string.Empty);
        var stream = AttributeText(Nearest(type, WellKnownTypeNames.EventStreamTypeAttribute), "All");
        if (source is null)
        {
            return new(null, "its raw observation filter is not constant text");
        }
        if (stream is null)
        {
            return new(null, "its raw observation filter is not constant text");
        }
        if (source.Length == 0)
        {
            return new(null, stream == "All" ? null : "a fixed stream with an unrestricted source has no observer-filter counterpart");
        }

        var selectedStream = stream == "All" ? null : stream;

        if (known.Where(item => item.StoredSource == source).ToList() is not [var definition])
        {
            return new(null, $"no unique definition stores source '{source}'");
        }

        return new(RawFilter(selectedStream, definition), null);
    }

    static ObservedEventSourceModel RawFilter(string? storedStream, ReactorSourceDefinition definition)
    {
        if (storedStream is null)
        {
            return new(definition.Source, null, true);
        }
        var names = definition.StoredStreams.Where(stream => stream.Value == storedStream).Select(stream => stream.Key).ToList();

        return names is [var name] ? new(definition.Source, name, true) : new(definition.Source, storedStream, false);
    }

    static string? AttributeText(AttributeData? attribute, string fallback) => attribute is null ? fallback : attribute.GetArgument(0) as string;

    static IEnumerable<INamedTypeSymbol> TypedDefinition(INamedTypeSymbol type) =>
        type.GetAttribute(WellKnownTypeNames.FromEventSourceAttributeOfT)?.AttributeClass is { TypeArguments: [INamedTypeSymbol definition] } ? [definition] : [];

    static List<string>? RolesOf(AttributeData? identity) => identity?.ConstructorArguments is [{ Kind: TypedConstantKind.Array, IsNull: false } argument] &&
        argument.Values.All(value => value.Value is string)
            ? argument.Values.Select(value => (string)value.Value!).ToList() : null;

    static bool IsDefinition(INamedTypeSymbol type) =>
        !type.IsAbstract && type.TypeKind is TypeKind.Class or TypeKind.Struct && type.FindInterface(WellKnownTypeNames.EventSourceDefinition) is not null;

    RecoveredObservation ReadObservation(INamedTypeSymbol type, List<ReactorSourceDefinition> known)
    {
        if (!type.HasAttribute(WellKnownTypeNames.FromEventSourceAttributeOfT))
        {
            return ReadRawObservation(type, known);
        }

        return EventSourceReader.ReadObserved(type, models) is { } observed
            ? new(NormalizeTypedObservation(observed, type.GetAttribute(WellKnownTypeNames.FromEventSourceAttributeOfT)?.GetArgument(0) as string), null)
            : new(null, "its typed observation definition could not be resolved");
    }

    ReactorSourceDefinition Describe(INamedTypeSymbol definition)
    {
        var streams = definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
            .Select(attribute => (Name: EventSourceNames.Stream(attribute, definition, models), Stored: attribute.GetArgument(0) as string ?? string.Empty))
            .Where(stream => stream.Name is not null).Select(stream => (Name: stream.Name!, stream.Stored)).ToList();

        return new(
            $"{definition.ContainingAssembly.Identity}:{definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}",
            EventSourceReader.SourceNameOf(definition, models),
            streams.ConvertAll(stream => stream.Name))
        {
            StoredSource = EventSourceNames.StoredSource(definition),
            StoredStreams = streams.DistinctBy(stream => stream.Name, StringComparer.Ordinal).ToDictionary(stream => stream.Name, stream => stream.Stored, StringComparer.Ordinal)
        };
    }

    List<ReactorSourceDefinition> DefinitionsFor(INamedTypeSymbol type) => definitions.Concat(TypedDefinition(type))
        .Where(IsDefinition).Select(Describe).DistinctBy(item => item.Identity, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Carries identity presence and refusal evidence without changing role text.
    /// </summary>
    /// <param name="Roles">The exact roles, or null when unavailable or absent.</param>
    /// <param name="Failure">The reason a present identity cannot be represented.</param>
    sealed record RecoveredIdentity(List<string>? Roles, string? Failure);

    /// <summary>
    /// Carries observation recovery independently of append routing.
    /// </summary>
    /// <param name="Filter">The recovered source and stream filter.</param>
    /// <param name="Failure">The reason observation cannot be represented.</param>
    sealed record RecoveredObservation(ObservedEventSourceModel? Filter, string? Failure);
}
