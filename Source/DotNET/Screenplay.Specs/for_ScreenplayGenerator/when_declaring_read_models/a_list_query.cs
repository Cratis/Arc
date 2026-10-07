// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model only ever read as a whole collection. Nothing reads one instance of it by a key, so nothing proves
/// what identifies an instance, and the declaration claims no identity.
/// </summary>
public class a_list_query : a_read_model_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author(string Name)
        {
            public static IEnumerable<Author> All() => [];
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_declare_the_read_model() => Says("readmodel Author").ShouldBeTrue();
    [Fact] void should_declare_what_it_holds() => Says("name String").ShouldBeTrue();
    [Fact] void should_claim_no_identifier() => Result.Source.ShouldNotContain("identifier");
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => RoundTrip.IsStable.ShouldBeTrue();
}
