// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_declared_graph_projection_names_collide : given.a_generated_document
{
    void Because() => Generate(given.GraphProjectionDeclarationSources.Files(
        given.GraphProjectionDeclarationSources.Marker("Directory", "typeof(global::Library.ReadModels.Graphs.Person)"),
        given.GraphProjectionDeclarationSources.Marker("Directory", "typeof(global::Library.ReadModels.Graphs.WORKS_AT)", "Library.Employment.Tracking")));

    [Fact] void should_keep_a_compilable_partial_document() => AssertPartialDocument();
    [Fact] void should_retain_the_shape() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_not_invent_names() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_report_the_name_collision() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph && diagnostic.Message.Contains("Directory", StringComparison.Ordinal)).ShouldBeTrue();
}
