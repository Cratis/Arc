// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_graph_declaration_has_an_unresolved_member : given.a_generated_document
{
    void Because() => Generate(given.GraphProjectionDeclarationSources.Files(
        given.GraphProjectionDeclarationSources.Marker(members: given.GraphProjectionDeclarationSources.AllMembers + ", typeof(MissingRecord)")));

    [Fact] void should_keep_the_recovered_shape() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_not_guess_a_projection() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_diagnose_unresolved_metadata() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("unresolved", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_still_round_trip_the_partial_output() => RoundTrip.IsStable.ShouldBeTrue();
    [Fact] void should_not_introduce_binding_errors() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeFalse();
}
