// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model holding a collection whose elements may be null. <c>optional</c> on a collection says only that the
/// collection may be absent, so no declaration can say what the read model holds - it is not declared, rather than
/// declared as holding a collection that may be missing and never holds a null.
/// </summary>
public class a_read_model_holding_a_collection_of_nullable_values : a_read_model_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        public record PenName(string?[] Variants);

        [ReadModel]
        public record Author(string Name, IEnumerable<PenName> PenNames)
        {
            public static IEnumerable<Author> All() => [];
        }

        [ReadModel]
        public record Pseudonym(string?[] Aliases)
        {
            public static IEnumerable<Pseudonym> AllPseudonyms() => [];
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_not_declare_the_read_model_holding_them() => Count("readmodel Pseudonym").ShouldEqual(0);
    [Fact] void should_not_declare_the_read_model_holding_them_further_down() => Count("readmodel Author").ShouldEqual(0);
    [Fact] void should_not_say_the_collection_is_optional() => Result.Source.ShouldNotContain("String[] optional");
    [Fact] void should_say_why() => LeftOut.Single(_ => _.Contains("'Pseudonym'")).ShouldContain("'Aliases'");
    [Fact] void should_name_the_value_further_down() => LeftOut.Single(_ => _.Contains("'Author'")).ShouldContain("'PenNames.Variants'");
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}
