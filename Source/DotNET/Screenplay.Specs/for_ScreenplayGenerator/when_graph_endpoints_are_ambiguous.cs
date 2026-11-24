// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_endpoints_are_ambiguous : given.a_generated_document
{
    void Because() => Generate(given.GraphSources.Files(given.GraphSources.Graph.Replace("[Target<Company>] string Target", "string Target", StringComparison.Ordinal)));

    [Fact] void should_keep_the_non_graph_document_compilable() => AssertPartialDocument();
    [Fact] void should_not_emit_a_misleading_graph() => Bound.Value.Model.Application.Graphs.ShouldBeEmpty();
    [Fact] void should_report_the_invalid_endpoint() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph).ShouldBeTrue();
}
