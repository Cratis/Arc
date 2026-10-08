// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

public class a_query_with_a_parameter_not_held_by_its_read_model : a_read_model_document
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Inventory.Listing;

        [ReadModel]
        public record Book(string Title)
        {
            public static Book? GetById(string id) => null;
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_preserve_the_query() => Says("query GetById => Book optional").ShouldBeTrue();
    [Fact] void should_preserve_the_caller_argument_as_a_filter() => Says("filter id String").ShouldBeTrue();
    [Fact] void should_not_claim_a_key_the_read_model_does_not_hold() => Result.Source.ShouldNotContain("by id");
    [Fact] void should_not_invent_a_read_model_property() => Says("id String").ShouldBeFalse();
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_report_no_binding_defect() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
}
