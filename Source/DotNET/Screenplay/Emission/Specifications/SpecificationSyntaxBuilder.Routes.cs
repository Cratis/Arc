// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

public partial class SpecificationSyntaxBuilder
{
    static bool HasNoStreamId(SpecificationEventRouteModel route) => route.StreamId is null && route.StreamIdParts.Count == 0;

    static TypeReferenceModel? StatedStreamIdType(SpecificationEventRouteModel route, CommandRouteModel? declared)
    {
        if (declared is not null)
        {
            return declared.StreamIdType;
        }

        return route.StreamId is not null ? new TypeReferenceModel("String", false, false) : null;
    }

    static bool UsesCommandIdentifiers(List<CommandModel> producers) =>
        producers.Count > 0 &&
        !producers.Exists(command => (command.Authoring?.Identifier ?? command.Identifier) is null || command.Produces.Any(produced => !produced.UsesCommandContext));

    bool CanStateRoute(SpecificationStateModel state, IEnumerable<SpecificationStateModel> occurrences)
    {
        if (state.Route is not { } route)
        {
            return true;
        }
        if (route.Source is null || route.Stream is null)
        {
            return route.Source is null && route.Stream is null && HasNoStreamId(route);
        }

        return route.Source != "Default" && CanStateNamedRoute(state, route, occurrences);
    }

    bool CanStateNamedRoute(SpecificationStateModel state, SpecificationEventRouteModel route, IEnumerable<SpecificationStateModel> occurrences)
    {
        var resolved = ResolveRoutes(occurrences);
        if (resolved.ConflictingSources.Contains(route.Identity!.Source))
        {
            return false;
        }

        var sourceDeclaration = resolved.Sources.SingleOrDefault(source => source.Identity.Source == route.Identity.Source);
        var declared = sourceDeclaration?.Streams.SingleOrDefault(stream => stream.Identity == route.Identity);

        return CanStateRouteSource(state, sourceDeclaration?.IdentifierType) && CanStateRouteStreamId(route, declared);
    }

    bool CanStateRouteSource(SpecificationStateModel state, TypeReferenceModel? identity)
    {
        if (identity is null)
        {
            return true;
        }

        var identifiers = ProducedIdentifierTypes(state.Name);

        return !identifiers.Exists(identifier => identifier != identity) && !(state.For is { } source && !CanStateLiteral(source, identity));
    }

    List<TypeReferenceModel> ProducedIdentifierTypes(string eventName) =>
        Application?.Slices.SelectMany(slice => slice.Commands)
            .Where(command => command.Produces.Any(production => production.EventName == eventName))
            .Select(IdentifierTypeOf)
            .OfType<TypeReferenceModel>().Distinct().ToList() ?? [];

    bool CanStateRouteStreamId(SpecificationEventRouteModel route, CommandRouteModel? declared)
    {
        var streamIdType = StatedStreamIdType(route, declared);
        if (declared?.StreamIdParts.Count > 0)
        {
            return streamIdType is null && CanStateStreamIdParts(route, declared);
        }
        if (streamIdType is null)
        {
            return HasNoStreamId(route);
        }

        return route.StreamIdParts.Count == 0 && route.StreamId is { } id && CanStateStreamId(id, streamIdType);
    }

    bool CanStateStreamIdParts(SpecificationEventRouteModel route, CommandRouteModel declared) =>
        route.StreamId is null && route.StreamIdParts.Count == declared.StreamIdParts.Count &&
        route.StreamIdParts.Zip(declared.StreamIdParts).All(pair => pair.First.Property == pair.Second.Name &&
            pair.First.Source is LiteralSource part && CanStateStreamId(part, pair.Second.Type));

    ResolvedEventSources ResolveRoutes(IEnumerable<SpecificationStateModel> occurrences)
    {
        if (Application is not { } application)
        {
            return new([], new HashSet<string>(StringComparer.Ordinal));
        }
        var routes = application.Slices.SelectMany(slice => slice.Commands).SelectMany(Commands.ProductionRouteAdmission.Routes);
        var stated = SpecificationRouteDeclarations.Read(application, (RoutedOccurrences ?? []).Concat(occurrences));

        return EventSourceDeclarations.Resolve(routes.Concat(stated), EventSourceDeclarations.ObservedRoutes(application, ReactionMetadataEnabled));
    }

    TypeReferenceModel? DeclaredIdentifierType(SpecificationEventRouteModel route) =>
        Application?.Slices.SelectMany(slice => slice.Commands)
            .SelectMany(Commands.ProductionRouteAdmission.Routes)
            .FirstOrDefault(candidate => candidate.Identity.HasSameSourceAs(route.Identity))?.IdentifierType;

    bool CanStateSource(SpecificationStateModel state)
    {
        var producers = Application?.Slices.SelectMany(slice => slice.Commands)
            .Where(command => command.Produces.Any(produced => naming.ToDeclarationName(produced.EventName) == naming.ToDeclarationName(state.Name)))
            .ToList() ?? [];
        if (CanStateRoutedSource(state, producers) is { } routed)
        {
            return routed;
        }

        return UsesCommandIdentifiers(producers) && CanStateProducedIdentifier(state, producers);
    }

    bool? CanStateRoutedSource(SpecificationStateModel state, List<CommandModel> producers)
    {
        if (!EventRoutes || state.Route is not { Source: not null } route)
        {
            return null;
        }

        if (DeclaredIdentifierType(route) is { } declaredType)
        {
            return CanStateLiteral(state.For!, declaredType);
        }

        return producers.Count == 0 ? state.For!.Value is string : null;
    }

    bool CanStateProducedIdentifier(SpecificationStateModel state, List<CommandModel> producers)
    {
        var identifiers = producers.ConvertAll(IdentifierTypeOf);
        if (identifiers.Exists(type => type is null or { IsOptional: true } or { IsCollection: true }) ||
            identifiers.Select(type => naming.ToDeclarationName(type!.Name)).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            return false;
        }

        return CanStateLiteral(state.For!, identifiers[0]!);
    }
}
