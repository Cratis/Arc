// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Reactors;

/// <summary>
/// Admits metadata only with exact names and genuine source definitions.
/// </summary>
/// <param name="application">The declaring application, if available.</param>
/// <param name="enabled">Whether ESM v10 metadata is admitted.</param>
internal class ReactionMetadataAdmission(ApplicationModel? application, bool enabled = true)
{
    /// <summary>
    /// Checks roles without sanitizing or removing them.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>Whether its identity can bind.</returns>
    internal static bool ValidIdentity(ReactorModel reactor) => reactor.IdentityFailure is null &&
        (reactor.SystemRoles is null || (reactor.SystemRoles.All(role => !string.IsNullOrWhiteSpace(role)) &&
            reactor.SystemRoles.Distinct(StringComparer.Ordinal).Count() == reactor.SystemRoles.Count()));

    /// <summary>
    /// Prevents execution when metadata would be discarded.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>Whether declarative effects retain their identity and scope.</returns>
    internal bool CanExecute(ReactorModel reactor) => ValidIdentity(reactor) && reactor.ObservationFailure is null &&
        (reactor.EventSource is null || SourceOf(reactor) is not null) &&
        (enabled || (reactor.SystemRoles is null && reactor.EventSource is null));

    /// <summary>
    /// Builds the representable filter independently of the body.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>The filter, or null when it cannot be represented.</returns>
    internal ObserverFilterSyntax? FilterOf(ReactorModel reactor) => enabled && SourceOf(reactor) is not null && reactor.EventSource is { } observed
        ? new(observed.Source, observed.Stream, SourceLocation.Start) : null;

    /// <summary>
    /// Resolves a unique definition without inventing identifier or stream-key types.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>The exact definition, or null.</returns>
    internal ReactorSourceDefinition? SourceOf(ReactorModel reactor)
    {
        if (reactor.ObservationFailure is not null || reactor.EventSource is not { StreamDeclared: true } observed || !ValidName(observed.Source) ||
            observed.Source == "Default" || (observed.Stream is not null && !ValidName(observed.Stream)))
        {
            return null;
        }

        var definitions = (application?.Slices.SelectMany(slice => slice.Reactors).SelectMany(item => item.ObservationDefinitions) ?? reactor.ObservationDefinitions)
            .DistinctBy(item => item.Identity, StringComparer.Ordinal).Where(item => item.Source == observed.Source).ToList();
        if (definitions is not [var definition] || definition.Streams.Any(stream => !ValidName(stream)) ||
            definition.Streams.Distinct(StringComparer.Ordinal).Count() != definition.Streams.Count ||
            (observed.Stream is not null && !definition.Streams.Contains(observed.Stream, StringComparer.Ordinal)))
        {
            return null;
        }

        var routes = application?.Slices.SelectMany(slice => slice.Commands).Select(command => command.Authoring?.Route)
            .OfType<CommandRouteModel>().Where(route => route.Source == observed.Source) ?? [];

        return routes.Any(route => route.Stream is not null && !definition.Streams.Contains(route.Stream, StringComparer.Ordinal)) ? null : definition;
    }

    /// <summary>
    /// Collects admitted observer-only declarations.
    /// </summary>
    /// <returns>The unique source definitions.</returns>
    internal IEnumerable<ReactorSourceDefinition> Sources() => !enabled || application is null ? [] : application.Slices.SelectMany(slice => slice.Reactors)
        .Select(SourceOf).OfType<ReactorSourceDefinition>().DistinctBy(item => item.Source, StringComparer.Ordinal);

    /// <summary>
    /// Reports metadata refusals while retaining the implementation source.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <param name="diagnostics">The diagnostic sink.</param>
    /// <param name="location">The declaring type or slice location.</param>
    internal void Report(ReactorModel reactor, ScreenplayDiagnostics diagnostics, string location)
    {
        if (!ValidIdentity(reactor))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrepresentableReactionIdentity, $"The reactor '{reactor.Name}' retains its source because {reactor.IdentityFailure ?? "its system roles are not non-blank distinct text"}", location);
        }
        if ((reactor.EventSource is not null && SourceOf(reactor) is null) || reactor.ObservationFailure is not null)
        {
            ReportObservation(reactor, diagnostics, location);
        }
        if (reactor.HasUnrepresentableAppendRoute)
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute, $"Any events returned by reactor '{reactor.Name}' retain their source implementation: append routing or providers cannot bind as routed reaction productions; observation metadata does not select an append route", location);
        }
        if (!enabled && (reactor.SystemRoles is not null || reactor.EventSource is not null))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.ReactionMetadataVersionCap, $"The reactor '{reactor.Name}' retains its source because runs as and from require ESM v10; no unauthorized or unfiltered executable effect was substituted", location);
        }
    }

    static void ReportObservation(ReactorModel reactor, ScreenplayDiagnostics diagnostics, string location)
    {
        if (reactor.EventSource is not null)
        {
            new ObservedEventSourceReport(diagnostics).Report("reactor", reactor.Name, reactor.EventSource, location);
        }
        else
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable, $"The reactor '{reactor.Name}' retains its source because {reactor.ObservationFailure}; no unfiltered executable effect was substituted", location);
        }
    }

    static bool ValidName(string name) => ScreenplayIdentifier.IsBareIdentifier(name) && !name.Any(char.IsWhiteSpace);
}
