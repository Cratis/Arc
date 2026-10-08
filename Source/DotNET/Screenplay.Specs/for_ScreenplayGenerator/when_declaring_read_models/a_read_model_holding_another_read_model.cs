// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model holding a value whose type is itself a read model. The value would have to be declared as a type
/// under the name the other read model is declared under, so the holding read model is not declared, and the one it
/// holds is declared once.
/// </summary>
public class a_read_model_holding_another_read_model : a_read_model_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author(string Name)
        {
            public static IEnumerable<Author> AllAuthors() => [];
        }

        [ReadModel]
        public record Shelf(string Label, Author Owner)
        {
            public static IEnumerable<Shelf> AllShelves() => [];
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_not_declare_the_holding_read_model() => Count("readmodel Shelf").ShouldEqual(0);
    [Fact] void should_declare_the_held_one_once() => Count("readmodel Author").ShouldEqual(1);
    [Fact] void should_not_declare_it_as_a_type() => Result.Source.ShouldNotContain("type Author");
    [Fact] void should_say_which_property() => LeftOut.Single().ShouldContain("'Owner'");
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}
