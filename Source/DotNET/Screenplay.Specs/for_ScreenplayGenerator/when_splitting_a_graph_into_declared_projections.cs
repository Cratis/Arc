// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_splitting_a_graph_into_declared_projections : given.a_generated_document
{
    void Because() => Generate(given.GraphProjectionDeclarationSources.Files(
        given.GraphProjectionDeclarationSources.Marker("PeopleDirectory", "typeof(global::Library.ReadModels.Graphs.Person)"),
        given.GraphProjectionDeclarationSources.Marker("EmploymentGraph", "typeof(global::Library.ReadModels.Graphs.WORKS_AT)", "Library.Employment.Tracking")));

    [Fact] void should_compile_bind_and_round_trip() => AssertDocument();
    [Fact] void should_keep_one_shared_shape() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_keep_both_named_projections() => Bound.Value.Model.Application.GraphProjections.Select(projection => projection.Name).Order(StringComparer.Ordinal).ShouldEqual(["EmploymentGraph", "PeopleDirectory"]);
    [Fact] void should_keep_node_handlers_in_the_node_owner() => Bound.Value.Model.Application.GraphProjections.Single(projection => projection.Name == "PeopleDirectory").Edges.ShouldBeEmpty();
    [Fact] void should_keep_edge_handlers_in_the_edge_owner() => Bound.Value.Model.Application.GraphProjections.Single(projection => projection.Name == "EmploymentGraph").Nodes.ShouldBeEmpty();
    [Fact] void should_keep_all_edge_handlers() => Bound.Value.Model.Application.GraphProjections.Single(projection => projection.Name == "EmploymentGraph").Edges.Single().Handlers.Length.ShouldEqual(2);
    [Fact] void should_create_the_second_marker_only_slice() => Result.Source.ShouldContain("slice StateView Tracking");
    [Fact] void should_not_warn_about_canonical_fallback() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.CanonicalGraphPlacement).ShouldBeFalse();
}
