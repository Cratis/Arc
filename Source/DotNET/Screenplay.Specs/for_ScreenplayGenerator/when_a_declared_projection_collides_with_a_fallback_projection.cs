// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_declared_projection_collides_with_a_fallback_projection : given.a_generated_document
{
    void Because() => Generate(
        [.. given.GraphProjectionDeclarationSources.Files(given.GraphProjectionDeclarationSources.Marker("Changes")),
        ("Alternate.cs", given.GraphSources.Graph.Replace("namespace Library.ReadModels.Graphs", "namespace Library.OtherGraphs", StringComparison.Ordinal).Replace("\"People\"", "\"Alternate\"", StringComparison.Ordinal))]);

    [Fact] void should_keep_a_compilable_partial_document() => AssertPartialDocument();
    [Fact] void should_keep_both_shapes() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(2);
    [Fact] void should_withhold_both_colliding_projections() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_report_the_actual_name_collision() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph && diagnostic.Message.Contains("Changes", StringComparison.Ordinal)).ShouldBeTrue();
}
