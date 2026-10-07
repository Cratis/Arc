// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_default_in_a_helper : a_generated_document
{
    [Theory]
    [InlineData("public AuthorRegistered Missing() => default!;", 4)]
    [InlineData("public AuthorRegistered Missing() { return default; }", 3)]
    [InlineData("public void Missing() { AuthorRegistered e = default!; }", 3)]
    [InlineData("public AuthorRegistered Missing() => default(AuthorRegistered)!;", 4)]
    public void should_keep_the_event_standalone_and_count_every_event_typed_expression(string helper, int expectedCount)
    {
        var helperSource = $$"""
            using Library.Authors.Registration;
            namespace Library.Authors.Helpers;
            public class Helpers
            {
                {{helper}}
            }
            """;
        Generate(
            (Analyzed.SlicePath, IdentifierSources.With("""
                [Command]
                public record RegisterAuthor(AuthorId Id, string Name)
                {
                    public AuthorRegistered Handle() => new(Name);
                }
                """)),
            ("Library/Authors/Helpers/Missing.cs", helperSource));
        Result.Source.ShouldNotContain("produces event AuthorRegistered");
        Result.Model.EventProducerCounts.Values.Single().ShouldEqual(expectedCount);
        AssertDocument();
    }
}
