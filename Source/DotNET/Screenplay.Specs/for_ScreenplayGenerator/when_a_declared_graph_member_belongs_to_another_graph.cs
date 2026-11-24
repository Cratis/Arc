// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_declared_graph_member_belongs_to_another_graph : given.a_generated_document
{
    void Because() => Generate(
        [.. given.GraphProjectionDeclarationSources.Files(given.GraphProjectionDeclarationSources.Marker(members: given.GraphProjectionDeclarationSources.AllMembers + ", typeof(global::Library.OtherGraphs.Company)")),
        ("Alternate.cs", given.GraphSources.Graph.Replace("namespace Library.ReadModels.Graphs", "namespace Library.OtherGraphs", StringComparison.Ordinal).Replace("\"People\"", "\"Alternate\"", StringComparison.Ordinal))]);

    [Fact] void should_keep_a_compilable_partial_document() => AssertPartialDocument();
    [Fact] void should_keep_both_shapes() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(2);
    [Fact] void should_keep_only_the_unaffected_graph_projection() => Bound.Value.Model.Application.GraphProjections.Single().Name.ShouldEqual("Changes");
    [Fact] void should_identify_the_wrong_graph_member() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("Library.OtherGraphs.Company", StringComparison.Ordinal)).ShouldBeTrue();
}
