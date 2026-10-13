// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_parenthesized_generation_argument : a_generation_strategy
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_recover_a_parenthesized_inline_uuid_without_a_strategy_line(bool capped)
    {
        var source = Source("Guid.NewGuid()", "Guid").Replace(
            "public AuthorRegistered Handle([GeneratedValue(\"receipt\")] AuthorId value) => new(Name, value);",
            "public AuthorRegistered Handle() { var receipt = new AuthorId(((Guid.NewGuid()))); return new(Name, receipt); }",
            StringComparison.Ordinal);
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = capped ? SemanticVersion.V14 : null }, (Analyzed.SlicePath, source));
        Result.Source.ShouldContain("receipt AuthorId generated");
        Result.Source.ShouldContain("receipt = receipt");
        Result.Source.ShouldNotContain("generate uuid");
        AssertDocument();
    }

    [Fact]
    void should_recover_a_parenthesized_factory_argument()
    {
        Generate((Analyzed.SlicePath, Source("((GenerateValue.UuidV7()))", "Guid")));
        AssertStrategy("generate uuidv7");
    }

    [Fact]
    void should_still_reject_a_cast_argument()
    {
        var source = Source("((Guid)Guid.NewGuid())", "Guid").Replace("AuthorId Receipt", "AuthorId? Receipt", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain(" generated");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).Message.ShouldContain("not a recognized");
        AssertDocument();
    }
}
