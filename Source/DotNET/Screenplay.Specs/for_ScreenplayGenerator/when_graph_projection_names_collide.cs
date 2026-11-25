// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_projection_names_collide : given.a_generated_document
{
    void Because() => Generate(
        [.. given.GraphSources.Files(),
        ("Library/OtherGraphs/Graph.cs", given.GraphSources.Graph
            .Replace("namespace Library.ReadModels.Graphs", "namespace Library.OtherGraphs", StringComparison.Ordinal)
            .Replace("\"People\"", "\"Alternate\"", StringComparison.Ordinal))]);

    [Fact] void should_keep_the_document_compilable() => AssertPartialDocument();
    [Fact] void should_keep_both_shapes() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(2);
    [Fact] void should_report_the_canonical_name_collision() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph).ShouldBeTrue();
    [Fact] void should_not_invent_projection_names() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
}
