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
internal class ReactorMetadataReader(IEnumerable<INamedTypeSymbol> definitions)
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
    /// <returns>Whether returned events require an unsupported append route.</returns>
    internal static bool RoutesAppends(INamedTypeSymbol type) =>
        type.FindInterface(WellKnownTypeNames.CanProvideEventSourceId) is not null ||
        type.FindInterface(WellKnownTypeNames.CanProvideEventStreamId) is not null ||
        (Nearest(type, WellKnownTypeNames.EventSourceTypeAttribute) is { } source && source.GetArgument(0) is not "Default") ||
        (Nearest(type, WellKnownTypeNames.EventStreamTypeAttribute) is { } stream && stream.GetArgument(0) is not "All") ||
        (Nearest(type, WellKnownTypeNames.EventStreamIdAttribute) is { } id && id.GetArgument(0) is not "" and not "Default");

    /// <summary>
    /// Adds metadata even when every handler remains code.
    /// </summary>
    /// <param name="type">The reactor type.</param>
    /// <param name="reactor">The body model.</param>
    /// <returns>The reactor with recovered metadata and explicit refusal evidence.</returns>
    internal ReactorModel Read(INamedTypeSymbol type, ReactorModel reactor)
    {
        var identity = Nearest(type, WellKnownTypeNames.ExecuteCommandsAsSystemAttribute);
        var roles = RolesOf(identity);
        var typed = type.GetAttribute(WellKnownTypeNames.FromEventSourceAttributeOfT);
        var observed = EventSourceReader.ReadObserved(type);
        if (observed is { StreamDeclared: true } declared && string.Equals(declared.Stream, "All", StringComparison.Ordinal))
        {
            observed = observed with { Stream = null };
        }
        var known = definitions.Concat(typed?.AttributeClass is { TypeArguments: [INamedTypeSymbol definition] } ? [definition] : [])
            .Where(IsDefinition).Select(Describe).DistinctBy(item => item.Identity, StringComparer.Ordinal).ToList();
        string? failure = null;
        if (typed is not null && observed is null)
        {
            failure = "its typed observation definition could not be resolved";
        }
        else if (typed is null)
        {
            (observed, failure) = ReadRawObservation(type, known);
        }

        return reactor with
        {
            EventSource = observed,
            SystemRoles = roles,
            IdentityFailure = identity?.AttributeClass is not null && (roles is not { } validRoles || validRoles.Exists(string.IsNullOrWhiteSpace) || validRoles.Distinct(StringComparer.Ordinal).Count() != validRoles.Count)
                ? "system roles must be non-null, non-blank, distinct text; no roles were sanitized" : null,
            ObservationFailure = failure,
            ObservationDefinitions = known,
            HasUnrepresentableAppendRoute = RoutesAppends(type),
            MetadataLocation = type.ToDisplayString()
        };
    }

    static (ObservedEventSourceModel? Filter, string? Failure) ReadRawObservation(INamedTypeSymbol type, List<ReactorSourceDefinition> known)
    {
        var sourceAttribute = Nearest(type, WellKnownTypeNames.EventSourceTypeAttribute);
        var streamAttribute = Nearest(type, WellKnownTypeNames.EventStreamTypeAttribute);
        if ((sourceAttribute?.GetArgument(0) is not string && sourceAttribute is not null) ||
            (streamAttribute?.GetArgument(0) is not string && streamAttribute is not null))
        {
            return (null, "its raw observation filter is not constant text");
        }

        var source = sourceAttribute?.GetArgument(0) as string ?? string.Empty;
        var stream = streamAttribute?.GetArgument(0) as string ?? "All";
        if (source.Length == 0)
        {
            return (null, stream == "All" ? null : "a fixed stream with an unrestricted source has no observer-filter counterpart");
        }

        var matches = known.Where(item => item.Source == source).ToList();
        var declared = stream == "All" || (matches is [var match] && match.Streams.Contains(stream, StringComparer.Ordinal));

        return (new(source, stream == "All" ? null : stream, declared), null);
    }

    static List<string>? RolesOf(AttributeData? identity) => identity?.ConstructorArguments is [{ Kind: TypedConstantKind.Array, IsNull: false } argument] &&
        argument.Values.All(value => value.Value is string)
            ? argument.Values.Select(value => (string)value.Value!).ToList() : null;

    static bool IsDefinition(INamedTypeSymbol type) =>
        !type.IsAbstract && type.TypeKind is TypeKind.Class or TypeKind.Struct && type.FindInterface(WellKnownTypeNames.EventSourceDefinition) is not null;

    static ReactorSourceDefinition Describe(INamedTypeSymbol definition) => new(
        $"{definition.ContainingAssembly.Identity}:{definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}",
        EventSourceReader.SourceNameOf(definition),
        definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute).Select(attribute => attribute.GetArgument(0) as string ?? string.Empty).ToList());
}
