// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_canonical_graph_names_collide_across_slices : given.a_generated_document
{
    const string People = """
        using Cratis.Chronicle.Events;
        namespace Library.People.Changes;
        [EventType("PersonRegistered")] public record PersonRegistered(string Id);
        """;
    const string Companies = """
        using Cratis.Chronicle.Events;
        namespace Library.Companies.Changes;
        [EventType("CompanyRegistered")] public record CompanyRegistered(string Id);
        """;
    const string Graphs = """
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.Graphs;
        using Cratis.Chronicle.Projections.ModelBound;
        namespace Library.Graphs;
        [Graph("People"), Node, FromEvent<Library.People.Changes.PersonRegistered>]
        public record Person([Key] string Id);
        [Graph("Companies"), Node, FromEvent<Library.Companies.Changes.CompanyRegistered>]
        public record Company([Key] string Id);
        """;

    string _first = string.Empty;

    void Because()
    {
        Generate(("Library/People/Changes/Event.cs", People), ("Library/Companies/Changes/Event.cs", Companies), ("Library/Graphs/Graphs.cs", Graphs));
        _first = Result.Source;
        Generate(("Library/Graphs/Graphs.cs", Graphs), ("Library/Companies/Changes/Event.cs", Companies), ("Library/People/Changes/Event.cs", People));
    }

    [Fact] void should_keep_the_document_compilable() => AssertPartialDocument();
    [Fact] void should_keep_both_graph_shapes() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(2);
    [Fact] void should_withhold_both_colliding_projections_without_renaming() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_diagnose_both_colliding_placements() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph).ShouldEqual(2);
    [Fact] void should_not_depend_on_source_order() => Result.Source.ShouldEqual(_first);
}
