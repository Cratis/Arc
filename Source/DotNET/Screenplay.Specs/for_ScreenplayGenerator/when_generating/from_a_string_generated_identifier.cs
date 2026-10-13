// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_string_generated_identifier : a_generated_document
{
    [Theory]
    [InlineData("GenerateValue.Ulid()")]
    [InlineData("GenerateValue.TypeId(\"author\")")]
    [InlineData("GenerateValue.NanoId()")]
    public void should_recover_a_string_event_source_identity(string call)
    {
        var source = GeneratedValueSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle([GeneratedValue("author")] AuthorId authorId) => (authorId, new(Name));
            }
            """)
            .Replace("AuthorId(Guid Value) : EventSourceId<Guid>(Value)", "AuthorId(string Value) : EventSourceId<string>(Value)", StringComparison.Ordinal)
            .Replace("new(Guid.NewGuid())", $"new({call})", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("author AuthorId generated identifier");
        Result.Source.ShouldContain("returns author");
        AssertDocument();
    }

    [Fact]
    void should_not_emit_a_low_entropy_generated_identifier()
    {
        var source = GeneratedValueSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId) => (authorId, new(Name));
            }
            """)
            .Replace("AuthorId(Guid Value) : EventSourceId<Guid>(Value)", "AuthorId(string Value) : EventSourceId<string>(Value)", StringComparison.Ordinal)
            .Replace("new(Guid.NewGuid())", "new(GenerateValue.NanoId(1))", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("concept AuthorId");
        Result.Source.ShouldNotContain(" generated");
        Result.Source.ShouldNotContain("returns authorId");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("PLAY0684", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
