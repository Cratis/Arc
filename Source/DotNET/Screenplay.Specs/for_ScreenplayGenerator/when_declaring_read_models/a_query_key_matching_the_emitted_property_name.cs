// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

public class a_query_key_matching_the_emitted_property_name : a_read_model_document
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Inventory.Listing;

        [ReadModel]
        public record Book(string ISBN_Value)
        {
            public static Book? ByIsbn(string isbnValue) => null;
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_use_the_same_name_for_the_property_and_argument() => Says("isbnValue String").ShouldBeTrue();
    [Fact] void should_key_the_query_by_the_emitted_property_name() => Says("by isbnValue String").ShouldBeTrue();
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_report_no_binding_defect() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
