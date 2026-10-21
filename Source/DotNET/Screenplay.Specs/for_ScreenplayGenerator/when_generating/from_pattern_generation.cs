// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_pattern_generation : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.Pattern(\"[A-Z]{16}\")", "generate pattern \"[A-Z]{16}\"")]
    [InlineData("GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Luhn)", "checksum luhn")]
    [InlineData("GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Mod11)", "checksum mod11")]
    [InlineData("GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Mod97)", "checksum mod97")]
    public void should_recover_the_finite_pattern_and_checksum(string call, string line)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        AssertStrategy(line);
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    }

    [Fact]
    void should_recover_dates_with_the_native_context()
    {
        var source = Source("GenerateValue.Pattern(\"{yyyy}{MM}[A-Z]{16}\", context)")
            .Replace("IGeneratable<AuthorId>", "IGeneratableInContext<AuthorId>", StringComparison.Ordinal)
            .Replace("AuthorId New()", "AuthorId New(GenerationContext context)", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        AssertStrategy("generate pattern \"{yyyy}{MM}[A-Z]{16}\"");
    }

    [Theory]
    [InlineData("{yyyy}[AB]{20}")]
    [InlineData("{yy}[AB]{20}")]
    [InlineData("{MM}[AB]{20}")]
    [InlineData("{dd}[AB]{20}")]
    public void should_refuse_dates_without_a_factory_context(string pattern)
    {
        Generate((Analyzed.SlicePath, Source($"GenerateValue.Pattern({JsonSerializer.Serialize(pattern)})")));
        Result.Source.ShouldNotContain("generate ");
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData("\\{yyyy\\}[AB]{20}")]
    [InlineData("[{MM}][AB]{20}")]
    public void should_not_treat_literal_braces_as_date_references(string pattern)
    {
        Generate((Analyzed.SlicePath, Source($"GenerateValue.Pattern({JsonSerializer.Serialize(pattern)})")));
        AssertStrategy("generate pattern");
    }

    [Theory]
    [InlineData("GenerateValue.Pattern(\"[\")")]
    [InlineData("GenerateValue.Pattern(\"[A-Z]{256}\", checksum: Checksum.Luhn)")]
    public void should_withhold_language_refuted_patterns(string call)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        Result.Source.ShouldNotContain("generate pattern");
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }
}
