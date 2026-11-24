// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_recovering_a_declared_graph_projection : given.a_generated_document
{
    void Because() => Generate(given.GraphProjectionDeclarationSources.Files(given.GraphProjectionDeclarationSources.Marker()));

    [Fact] void should_compile_and_bind_the_real_attribute() => AssertDocument();
    [Fact] void should_preserve_the_declared_name() => Bound.Value.Model.Application.GraphProjections.Single().Name.ShouldEqual("PeopleDirectory");
    [Fact] void should_create_the_marker_only_slice() => Result.Source.ShouldContain("slice StateView Directory");
    [Fact] void should_place_the_projection_in_the_declaring_slice() => new Cratis.Screenplay.ScreenplayCompiler().Compile(Result.Source).Value!.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Name == "Directory").GraphProjections.Single().Name.ShouldEqual("PeopleDirectory");
    [Fact] void should_keep_one_complete_graph() => Bound.Value.Model.Application.Graphs.Single().Nodes.Length.ShouldEqual(2);
    [Fact] void should_preserve_all_owned_handlers() => Bound.Value.Model.Application.GraphProjections.Single().Edges.Single().Handlers.Length.ShouldEqual(2);
    [Fact] void should_not_report_fallback_placement() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.CanonicalGraphPlacement).ShouldBeFalse();
}
