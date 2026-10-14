// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Resolves every declaration through the same model and stored route identities.
/// </summary>
internal static class EventSourceDeclarations
{
    /// <summary>
    /// Reads the routes implied by admitted observer declarations.
    /// </summary>
    /// <param name="model">The application declaring observers.</param>
    /// <param name="enabled">Whether reaction metadata is enabled.</param>
    /// <returns>The source and stream declarations required by observers.</returns>
    internal static IEnumerable<CommandRouteModel> ObservedRoutes(ApplicationModel model, bool enabled = true) =>
        new ReactionMetadataAdmission(model, enabled).Sources().SelectMany(ObservedRoutes);

    /// <summary>
    /// Refuses incompatible identities and combines compatible routes into unique declarations.
    /// </summary>
    /// <param name="routes">Command and specification routes, including their stream-id shapes.</param>
    /// <param name="observed">Observer declarations, which do not determine stream-id shapes.</param>
    /// <returns>The unique declarations and refused source names.</returns>
    internal static ResolvedEventSources Resolve(IEnumerable<CommandRouteModel> routes, IEnumerable<CommandRouteModel> observed)
    {
        var declared = routes.ToList();
        var sources = declared.Concat(observed).GroupBy(route => route.Identity.Source, StringComparer.Ordinal).ToList();
        var conflicts = declared.GroupBy(route => route.Identity.Source, StringComparer.Ordinal)
            .Where(IncompatibleShapes).Select(source => source.Key).ToHashSet(StringComparer.Ordinal);
        conflicts.UnionWith(sources.Where(IncompatibleNames).Select(source => source.Key));
        var admitted = sources.Where(source => !conflicts.Contains(source.Key)).Select(Describe).ToList();

        return new(admitted, conflicts);
    }

    static IEnumerable<CommandRouteModel> ObservedRoutes(ReactorSourceDefinition source)
    {
        // Chronicle's All sentinel means unrestricted observation, not a stored stream declaration.
        var streams = source.Streams.Where(stream => source.StoredStreams.GetValueOrDefault(stream) != "All").ToList();

        return streams.Count == 0 ? [new CommandRouteModel(source.Source, null, null, null, null) { StoredSource = source.StoredSource }] :
            streams.Select(stream => new CommandRouteModel(source.Source, stream, null, null, null)
            {
                StoredSource = source.StoredSource,
                StoredStream = source.StoredStreams.GetValueOrDefault(stream)
            });
    }

    static bool IncompatibleShapes(IGrouping<string, CommandRouteModel> source) =>
        source.Select(route => route.IdentifierType).OfType<TypeReferenceModel>().Distinct().Count() > 1 ||
        source.GroupBy(route => route.Identity.Stream, StringComparer.Ordinal)
            .Any(stream => stream.Any(route => !CommandRouteAdmission.SameStream(stream.First(), route)));

    static bool IncompatibleNames(IGrouping<string, CommandRouteModel> source) =>
        source.Select(route => route.Identity.StoredSource).Distinct(StringComparer.Ordinal).Count() > 1 ||
        source.GroupBy(route => route.Identity.Stream, StringComparer.Ordinal).Any(stream =>
            stream.Select(route => route.Identity.StoredStream).Distinct(StringComparer.Ordinal).Count() > 1);

    static EventSourceDeclaration Describe(IGrouping<string, CommandRouteModel> source) => new(
        source.First().Identity with { Stream = null, StoredStream = null },
        source.Select(route => route.IdentifierType).OfType<TypeReferenceModel>().FirstOrDefault(),
        source.Where(route => route.Identity.Stream is not null).GroupBy(route => route.Identity).Select(stream => stream.First()).ToList());
}
