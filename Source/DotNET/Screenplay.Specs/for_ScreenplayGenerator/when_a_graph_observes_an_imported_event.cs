// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_graph_observes_an_imported_event : Specification
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

    void Because() => _analysis = Analyzed.SourceReferencing(Analyzed.Package("External", Event), ("Library/Graphs/People.cs", Graph));

    [Fact] void should_not_emit_a_projection_for_imported_only_handlers() => new ScreenplayEmitter().Emit(_analysis.Model, new ScreenplayOptions()).Application.Modules
        .SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.GraphProjections).ShouldBeEmpty();
    [Fact] void should_not_fabricate_an_imported_event_slice() => _analysis.Model.Slices.Any(slice => slice.Namespace.StartsWith("External.", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_retain_the_shape_in_its_declaring_namespace() => _analysis.Model.Graphs.Single().Namespace.ShouldEqual("Library.Graphs");
    [Fact] void should_withhold_projection_placement_without_a_local_declaring_event_slice() => _analysis.Model.Graphs.Single().HasProjectionPlacement.ShouldBeFalse();
    [Fact] void should_diagnose_the_absent_declaring_event_slice() => _analysis.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph).ShouldBeTrue();
    [Fact] void should_import_a_graph_only_dependency() => _analysis.Model.Imports.ShouldContain("External.People.Registered.Registered");
    [Fact] void should_not_redeclare_the_imported_event() => _analysis.Model.Slices.SelectMany(slice => slice.Events).ShouldBeEmpty();
    [Fact] void should_recover_the_handler_reference() => _analysis.Model.Graphs.Single().Elements.Single().Handlers.Single().Event.ShouldEqual("Registered");
    [Fact] void should_not_report_an_unresolved_event() => _analysis.Diagnostics.All(diagnostic => diagnostic.Code != ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation).ShouldBeTrue();
}
