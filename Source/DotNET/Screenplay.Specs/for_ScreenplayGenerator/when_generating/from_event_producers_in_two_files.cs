// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_event_producers_in_two_files : a_generated_document
{
    const string OtherSource = """
        using Cratis.Arc.Commands.ModelBound;
        using Library.Authors.Registration;
        namespace Library.Authors.Renaming;
        [Command]
        public record RenameAuthor(AuthorId Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    void Because() => Generate(
        (Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)),
        ("Library/Authors/Renaming/Rename.cs", OtherSource));

    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_declare_the_event_once() => Result.Model.Slices.SelectMany(_ => _.Events).Count().ShouldEqual(1);
    [Fact] void should_count_both_sites() => Result.Model.EventProducerCounts.Values.Single().ShouldEqual(2);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
