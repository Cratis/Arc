// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

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

    [Fact] void should_import_a_graph_only_dependency() => _analysis.Model.Imports.ShouldContain("External.People.Registered.Registered");
    [Fact] void should_not_redeclare_the_imported_event() => _analysis.Model.Slices.SelectMany(slice => slice.Events).ShouldBeEmpty();
    [Fact] void should_recover_the_handler_reference() => _analysis.Model.Graphs.Single().Elements.Single().Handlers.Single().Event.ShouldEqual("Registered");
    [Fact] void should_not_report_an_unresolved_event() => _analysis.Diagnostics.All(diagnostic => diagnostic.Code != ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation).ShouldBeTrue();
}
