// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_graph_declaration_owns_only_shape_records : given.a_generated_document
{
    void Because() => Generate(given.GraphProjectionDeclarationSources.Files(
        given.GraphProjectionDeclarationSources.Marker(),
        given.GraphProjectionDeclarationSources.Marker("CompanyView", "typeof(global::Library.ReadModels.Graphs.Company)", "Library.People.Company")));

    [Fact] void should_keep_a_compilable_partial_document() => AssertPartialDocument();
    [Fact] void should_keep_the_shared_graph_shape() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_withhold_invalid_explicit_projections_without_fallback() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_identify_the_handler_less_declaration() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("declaration owns no handler-bearing node or edge", StringComparison.Ordinal) && diagnostic.Message.Contains("Library.People.Company", StringComparison.Ordinal)).ShouldBeTrue();
}
