// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_declared_graph_observes_only_imported_events : Specification
{
    const string Event = """
        using Cratis.Chronicle.Events;
        namespace External.People.Registered;
        [EventType("Registered")] public record Registered(string Id);
        """;
    const string Graph = """
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.Graphs;
        using Cratis.Chronicle.Projections.ModelBound;
        using External.People.Registered;
        namespace Library.Graphs;
        [Graph("People"), Node, FromEvent<Registered>]
        public record Person([Key] string Id);
        """;
    ApplicationModelAnalysis _analysis;
    ApplicationSyntax _application;

    void Because()
    {
        _analysis = Analyzed.SourceReferencing(
            Analyzed.Package("External", Event),
            ("Graph.cs", Graph),
            ("Declaration.cs", given.GraphProjectionDeclarationSources.Marker(members: "typeof(global::Library.Graphs.Person)")));
        _application = new ScreenplayEmitter().Emit(_analysis.Model, new ScreenplayOptions()).Application;
    }

    [Fact] void should_place_the_projection_in_the_marker_slice() => _application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Name == "Directory").GraphProjections.Single().Name.ShouldEqual("PeopleDirectory");
    [Fact] void should_keep_the_event_import() => _analysis.Model.Imports.ShouldContain("External.People.Registered.Registered");
    [Fact] void should_not_invent_a_local_event_slice() => _analysis.Model.Slices.Any(slice => slice.Namespace.StartsWith("External.", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_diagnose_missing_fallback_placement() => _analysis.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph || diagnostic.Code == ScreenplayDiagnosticCodes.CanonicalGraphPlacement).ShouldBeFalse();
}
