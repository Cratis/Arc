// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_v21_generation_caps : a_generation_strategy
{
    [Theory]
    [InlineData(false, 20)]
    [InlineData(true, 20)]
    [InlineData(false, 21)]
    [InlineData(true, 21)]
    public void should_share_the_cap_for_each_v21_strategy(bool authoring, int cap)
    {
        foreach (var (call, primitive) in new[] { ("GenerateValue.Pattern(\"[AB]{20}\")", "string"), ("GenerateValue.Integer(1, 9)", "long"), ("string.Concat(\"prefix\", Guid.NewGuid().ToString())", "string") })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = cap == 20 ? SemanticVersion.V20 : SemanticVersion.V21 },
                (Analyzed.SlicePath, Source(call, primitive)));
            Result.Source.Contains("receipt AuthorId generated", StringComparison.Ordinal).ShouldEqual(cap == 21);
            if (cap == 20)
            {
                Result.Source.ShouldNotContain("generate ");
                Result.Source.ShouldNotContain("receipt = receipt");
                Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable && diagnostic.Message.Contains("ESM v21", StringComparison.Ordinal)).ShouldBeTrue();
            }
            AssertPartialDocument();
        }
    }

    [Theory]
    [InlineData("GenerateValue.Pattern(\"[AB]{20}\")", "string", false)]
    [InlineData("GenerateValue.Pattern(\"[AB]{64}\")", "string", true)]
    [InlineData("GenerateValue.Integer(1, 9)", "long", false)]
    [InlineData("GenerateValue.Integer(-9223372036854775808, 9223372036854775807)", "long", false)]
    [InlineData("string.Concat(\"prefix\", Guid.NewGuid().ToString())", "string", false)]
    public void should_ask_the_binder_for_identifier_entropy_and_refuse_opaque_rules(string call, string primitive, bool admitted)
    {
        var source = Source(call, primitive).Replace(
            "public AuthorRegistered Handle([GeneratedValue(\"receipt\")] AuthorId value) => new(Name, value);",
            "public (AuthorId, AuthorRegistered) Handle([GeneratedValue(\"receipt\")] AuthorId value) => (value, new(Name, value));",
            StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Assert.True(Result.Source.Contains("receipt AuthorId generated identifier", StringComparison.Ordinal) == admitted, $"{Result.Source}\n{string.Join('\n', Result.Diagnostics.Select(diagnostic => diagnostic.Message))}");
        if (!admitted)
        {
            Result.Source.ShouldNotContain("produces AuthorRegistered");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        }
        AssertPartialDocument();
    }
}
