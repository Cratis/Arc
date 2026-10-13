// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Keeps declaration compatibility in both model and stored name spaces.
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
        new ReactionMetadataAdmission(model, enabled).Sources().SelectMany(source =>
            source.Streams.Count == 0 ? [new CommandRouteModel(source.Source, null, null, null, null) { StoredSource = source.StoredSource }] :
                source.Streams.Select(stream => new CommandRouteModel(source.Source, stream, null, null, null)
                {
                    StoredSource = source.StoredSource,
                    StoredStream = source.StoredStreams.GetValueOrDefault(stream)
                }));

    /// <summary>
    /// Finds model names that would merge incompatible runtime destinations.
    /// </summary>
    /// <param name="routes">The command and observer declarations.</param>
    /// <returns>The source names whose stored identities conflict.</returns>
    internal static IEnumerable<string> ConflictingStoredNames(IEnumerable<CommandRouteModel> routes) => routes
        .GroupBy(route => route.Source, StringComparer.Ordinal).Where(source =>
            source.Select(route => route.StoredSource ?? route.Source).Distinct(StringComparer.Ordinal).Count() > 1 ||
            source.GroupBy(route => route.Stream, StringComparer.Ordinal).Any(stream =>
                stream.Select(route => route.StoredStream ?? route.Stream).Distinct(StringComparer.Ordinal).Count() > 1))
        .Select(source => source.Key);
}
