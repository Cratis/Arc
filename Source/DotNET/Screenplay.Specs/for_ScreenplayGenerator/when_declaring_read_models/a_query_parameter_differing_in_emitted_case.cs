// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

public class a_query_parameter_differing_in_emitted_case : a_read_model_document
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Inventory.Listing;

        [ReadModel]
        public record Book(string BookId)
        {
            public static Book? ById(string bookID) => null;
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_keep_the_property_case() => Says("bookId String").ShouldBeTrue();
    [Fact] void should_keep_the_parameter_case_as_a_filter() => Says("filter bookID String").ShouldBeTrue();
    [Fact] void should_not_key_by_a_name_the_binder_would_not_resolve() => Result.Source.ShouldNotContain("by bookID");
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_report_no_binding_defect() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
}
