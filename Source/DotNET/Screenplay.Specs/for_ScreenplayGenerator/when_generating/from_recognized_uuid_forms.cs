// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_recognized_uuid_forms : a_generated_document
{
    [Theory]
    [InlineData("public static AuthorId New() => new(Guid.NewGuid());")]
    [InlineData("public static AuthorId New() { return new AuthorId(Guid.NewGuid()); }")]
    [InlineData("public static AuthorId New() => new(GenerateValue.Uuid());")]
    [InlineData("static AuthorId IGeneratable<AuthorId>.New() => new(Guid.NewGuid());")]
    [InlineData("public static AuthorId New() => Guid.NewGuid(); public static implicit operator AuthorId(Guid value) => new(value);")]
    [InlineData("public static AuthorId New() => GenerateValue.Uuid(); public static implicit operator AuthorId(Guid value) { return new(value); }")]
    public void should_recover_only_the_proven_uuid_v4_contract(string factory)
    {
        var source = GeneratedValueSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId) => (authorId, new(Name));
            }
            """).Replace("public static AuthorId New() => new(Guid.NewGuid());", factory, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("authorId AuthorId generated identifier");
        Result.Source.ShouldContain("returns authorId");
        Result.Source.ShouldNotContain("generate uuid");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeEmpty();
        AssertDocument();
    }
}
