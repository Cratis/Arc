// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_declared_graph_projections_share_a_slice : given.a_generated_document
{
    void Because() => Generate(
        [.. given.GraphProjectionDeclarationSources.Files(
            given.GraphProjectionDeclarationSources.Marker("PeopleDirectory", "typeof(global::Library.ReadModels.Graphs.Person)", type: "FirstMarker"),
            given.GraphProjectionDeclarationSources.Marker("EmploymentGraph", "typeof(global::Library.ReadModels.Graphs.Employment)", type: "SecondMarker"))
        .Select(file => (file.Path, file.Text.Replace("record WORKS_AT", "record Employment", StringComparison.Ordinal)))]);

    [Fact] void should_compile_bind_and_round_trip() => AssertDocument();
    [Fact] void should_preserve_both_names_in_one_slice() => Bound.Value.Model.Application.GraphProjections.Length.ShouldEqual(2);
    [Fact] void should_keep_one_shape() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_use_symbol_ownership_not_the_edge_label() => Bound.Value.Model.Application.GraphProjections.Single(projection => projection.Name == "EmploymentGraph").Edges.Single().Name.ShouldEqual("WORKS_AT");
    [Fact] void should_keep_the_projection_off_the_event_slice() => Result.Source.ShouldContain("slice StateView Directory");
}
