// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_graph_projection_declaration_targets_an_unknown_graph : given.a_generated_document
{
    void Because() => Generate(given.GraphProjectionDeclarationSources.Files(given.GraphProjectionDeclarationSources.Marker(graph: "MissingGraph")));

    [Fact] void should_keep_the_unrelated_fallback_compilable() => AssertDocument();
    [Fact] void should_not_apply_the_marker_to_another_graph() => Bound.Value.Model.Application.GraphProjections.Single().Name.ShouldEqual("Changes");
    [Fact] void should_report_the_unknown_graph_and_marker() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("MissingGraph", StringComparison.Ordinal) && diagnostic.Message.Contains("UnrelatedMarker", StringComparison.Ordinal)).ShouldBeTrue();
}
