// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unproven_generated_values : a_generated_document
{
    [Theory]
    [InlineData("var ignored = authorId = new AuthorId(Guid.Empty);")]
    [InlineData("Reset(ref authorId);")]
    [InlineData("ResetOut(out authorId);")]
    [InlineData("Action change = () => authorId = new AuthorId(Guid.Empty);")]
    public void should_reject_generated_locals_written_after_initialization(string write)
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                static void Reset(ref AuthorId value) => value = new(Guid.Empty);
                static void ResetOut(out AuthorId value) => value = new(Guid.Empty);
                public (AuthorId, AuthorRegistered) Handle()
                {
                    var authorId = new AuthorId(Guid.NewGuid());
            """ + write + """
                    return (authorId, new AuthorRegistered(Name));
                }
            }
            """)));
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns authorId");
        Result.Source.ShouldNotContain("for authorId");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("written after its initializer", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData("new AuthorId(Guid.NewGuid())")]
    [InlineData("AuthorId.New()")]
    public void should_not_claim_freshness_through_a_non_passthrough_constructor(string creation)
    {
        const string Constructor = """
            public record AuthorId : EventSourceId<Guid>
            {
                public AuthorId(Guid value) : base(Guid.Empty) { }
                public static new AuthorId New() => new(Guid.NewGuid());
            }
            """;
        var source = IdentifierSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle()
                {
            """ + "var authorId = " + creation + ";" + """
                    return (authorId, new AuthorRegistered(Name));
                }
            }
            """).Replace("public record AuthorId(Guid Value) : EventSourceId<Guid>(Value);", Constructor, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns authorId");
        Result.Source.ShouldNotContain("for authorId");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeTrue();
        AssertDocument();
    }

    [Fact]
    public void should_recognize_an_explicit_passthrough_constructor()
    {
        const string Constructor = """
            public record AuthorId : EventSourceId<Guid>
            {
                public AuthorId(Guid value) : base(value) { }
            }
            """;
        var source = IdentifierSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public AuthorId Handle() => new AuthorId(Guid.NewGuid());
            }
            """).Replace("public record AuthorId(Guid Value) : EventSourceId<Guid>(Value);", Constructor, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("authorId AuthorId generated identifier");
        AssertDocument();
    }

    [Fact]
    public void should_not_copy_a_response_property_that_transforms_its_parameter()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            public record Receipt(string Name)
            {
                public string Name { get; init; } = Name.Trim();
            }
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorRegistered, Receipt) Handle() => (new(Name), new(Name));
            }
            """)));
        Result.Source.ShouldNotContain("returns");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeTrue();
        AssertDocument();
    }
}
