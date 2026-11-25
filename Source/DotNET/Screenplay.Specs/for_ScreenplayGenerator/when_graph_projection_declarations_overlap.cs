// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_projection_declarations_overlap : given.a_generated_document
{
    void Because() => Generate(given.GraphProjectionDeclarationSources.Files(
        given.GraphProjectionDeclarationSources.Marker(),
        given.GraphProjectionDeclarationSources.Marker("Other", location: "Library.People.Other")));

    [Fact] void should_keep_a_compilable_partial_document() => AssertPartialDocument();
    [Fact] void should_keep_the_graph_shape() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_withhold_the_entire_projection_set() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_name_the_conflicting_member_and_both_owners() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("already owned", StringComparison.Ordinal) && diagnostic.Message.Contains("Person", StringComparison.Ordinal)).ShouldBeTrue();
}
