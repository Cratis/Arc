// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model no slice refers to, written in a namespace that is no slice. There is no slice to declare it in, so it
/// is not declared, and nothing else in the document changes.
/// </summary>
public class a_read_model_nothing_refers_to : a_read_model_document
{
    const string Unused = """
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Leftovers;

        [ReadModel]
        public record Remnant(string Name);
        """;

    const string Listing = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author(string Name)
        {
            public static IEnumerable<Author> All() => [];
        }
        """;

    void Because() => Generate(("Library/Leftovers/Remnant.cs", Unused), ("Library/Authors/Listing/Listing.cs", Listing));

    [Fact] void should_not_declare_it() => Result.Source.ShouldNotContain("Remnant");
    [Fact] void should_say_why() => LeftOut.Single().ShouldContain("'Remnant' is neither referred to");
    [Fact] void should_still_declare_the_read_model_a_slice_reads() => Count("readmodel Author").ShouldEqual(1);
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}
