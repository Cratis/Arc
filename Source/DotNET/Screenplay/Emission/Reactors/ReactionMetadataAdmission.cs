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
    /// Distinguishes absence of a filter from admission and refusal of a declared filter.
    /// </summary>
    enum ObservationState
    {
        Unrestricted = 0,
        Admitted = 1,
        Refused = 2
    }

    /// <summary>
    /// Checks roles without sanitizing or removing them.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>Whether its identity can bind.</returns>
    internal static bool ValidIdentity(ReactorModel reactor)
    {
        if (reactor.IdentityFailure is not null)
        {
            return false;
        }
        if (reactor.SystemRoles is not { } roles)
        {
            return true;
        }

        return roles.All(role => !string.IsNullOrWhiteSpace(role)) && roles.Distinct(StringComparer.Ordinal).Count() == roles.Count();
    }

    /// <summary>
    /// Prevents execution when metadata would be discarded.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>Whether declarative effects retain their identity and scope.</returns>
    internal bool CanExecute(ReactorModel reactor)
    {
        if (!ValidIdentity(reactor))
        {
            return false;
        }
        if (ObservationOf(reactor) == ObservationState.Refused)
        {
            return false;
        }

        return !IsCapped(reactor);
    }

    /// <summary>
    /// Builds the representable filter independently of the body.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>The filter, or null when it cannot be represented.</returns>
    internal ObserverFilterSyntax? FilterOf(ReactorModel reactor)
    {
        if (!enabled)
        {
            return null;
        }
        if (reactor.EventSource is not { } observed)
        {
            return null;
        }

        return SourceOf(reactor) is not null ? new(observed.Source, observed.Stream, SourceLocation.Start) : null;
    }

    /// <summary>
    /// Resolves a unique definition without inventing identifier or stream-key types.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <returns>The exact definition, or null.</returns>
    internal ReactorSourceDefinition? SourceOf(ReactorModel reactor)
    {
        if (EligibleFilter(reactor) is not { } observed)
        {
            return null;
        }
        if (UniqueSource(reactor, observed.Source) is not { } definition)
        {
            return null;
        }
        if (!ValidStreams(definition))
        {
            return null;
        }
        if (!ObservesDeclaredStream(observed, definition))
        {
            return null;
        }

        return RoutesUseDeclaredStreams(definition) ? definition : null;
    }

    /// <summary>
    /// Collects admitted observer-only declarations.
    /// </summary>
    /// <returns>The unique source definitions.</returns>
    internal IEnumerable<ReactorSourceDefinition> Sources()
    {
        if (!enabled)
        {
            return [];
        }
        if (application is null)
        {
            return [];
        }

        return application.Slices.SelectMany(slice => slice.Reactors)
            .Select(SourceOf).OfType<ReactorSourceDefinition>().DistinctBy(item => item.Source, StringComparer.Ordinal);
    }

    /// <summary>
    /// Reports metadata refusals while retaining the implementation source.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <param name="diagnostics">The diagnostic sink.</param>
    /// <param name="location">The declaring type or slice location.</param>
    internal void Report(ReactorModel reactor, ScreenplayDiagnostics diagnostics, string location)
    {
        ReportIdentity(reactor, diagnostics, location);
        ReportObservation(reactor, diagnostics, location);
        ReportAppendRoute(reactor, diagnostics, location);
        ReportVersionCap(reactor, diagnostics, location);
    }

    static void ReportIdentity(ReactorModel reactor, ScreenplayDiagnostics diagnostics, string location)
    {
        if (!ValidIdentity(reactor))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrepresentableReactionIdentity, $"The reactor '{reactor.Name}' retains its source because {reactor.IdentityFailure ?? "its system roles are not non-blank distinct text"}", location);
        }
    }

    static void ReportAppendRoute(ReactorModel reactor, ScreenplayDiagnostics diagnostics, string location)
    {
        if (reactor.HasUnrepresentableAppendRoute)
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute, $"Any events returned by reactor '{reactor.Name}' retain their source implementation: append routing or providers cannot bind as routed reaction productions; observation metadata does not select an append route", location);
        }
    }

    static bool ValidName(string name) => ScreenplayIdentifier.IsBareIdentifier(name) && !name.Any(char.IsWhiteSpace);

    static bool ValidFilter(ObservedEventSourceModel observed)
    {
        if (observed.Source == "Default")
        {
            return false;
        }
        if (!ValidName(observed.Source))
        {
            return false;
        }

        return observed.Stream is null || ValidName(observed.Stream);
    }

    static ObservedEventSourceModel? EligibleFilter(ReactorModel reactor)
    {
        if (reactor.ObservationFailure is not null)
        {
            return null;
        }
        if (reactor.EventSource is not { StreamDeclared: true } observed)
        {
            return null;
        }

        return ValidFilter(observed) ? observed : null;
    }

    static bool ValidStreams(ReactorSourceDefinition definition) => definition.Streams.All(ValidName) &&
        definition.Streams.Distinct(StringComparer.Ordinal).Count() == definition.Streams.Count;

    static bool ObservesDeclaredStream(ObservedEventSourceModel observed, ReactorSourceDefinition definition) =>
        observed.Stream is null || definition.Streams.Contains(observed.Stream, StringComparer.Ordinal);

    static bool HasMetadata(ReactorModel reactor) => reactor.SystemRoles is not null || reactor.EventSource is not null;

    void ReportVersionCap(ReactorModel reactor, ScreenplayDiagnostics diagnostics, string location)
    {
        if (IsCapped(reactor))
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.ReactionMetadataVersionCap, $"The reactor '{reactor.Name}' retains its source because runs as and from require ESM v10; no unauthorized or unfiltered executable effect was substituted", location);
        }
    }

    void ReportObservation(ReactorModel reactor, ScreenplayDiagnostics diagnostics, string location)
    {
        if (ObservationOf(reactor) != ObservationState.Refused)
        {
            return;
        }
        if (reactor.EventSource is not null)
        {
            new ObservedEventSourceReport(diagnostics).Report("reactor", reactor.Name, reactor.EventSource, location);
        }
        else
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable, $"The reactor '{reactor.Name}' retains its source because {reactor.ObservationFailure}; no unfiltered executable effect was substituted", location);
        }
    }

    bool IsCapped(ReactorModel reactor) => !enabled && HasMetadata(reactor);

    ObservationState ObservationOf(ReactorModel reactor)
    {
        if (reactor.ObservationFailure is not null)
        {
            return ObservationState.Refused;
        }
        if (reactor.EventSource is null)
        {
            return ObservationState.Unrestricted;
        }

        return SourceOf(reactor) is null ? ObservationState.Refused : ObservationState.Admitted;
    }

    ReactorSourceDefinition? UniqueSource(ReactorModel reactor, string source) => DefinitionsOf(reactor)
        .DistinctBy(item => item.Identity, StringComparer.Ordinal).Where(item => item.Source == source).ToList() is [var definition] ? definition : null;

    IEnumerable<ReactorSourceDefinition> DefinitionsOf(ReactorModel reactor) => application is null
        ? reactor.ObservationDefinitions
        : application.Slices.SelectMany(slice => slice.Reactors).SelectMany(item => item.ObservationDefinitions);

    bool RoutesUseDeclaredStreams(ReactorSourceDefinition definition) => DeclaredRoutes()
        .Where(route => route.Source == definition.Source).Select(route => route.Stream).OfType<string>()
        .All(stream => definition.Streams.Contains(stream, StringComparer.Ordinal));

    IEnumerable<CommandRouteModel> DeclaredRoutes() => application is null ? [] : application.Slices.SelectMany(slice => slice.Commands)
        .Select(command => command.Authoring?.Route).OfType<CommandRouteModel>();
}
