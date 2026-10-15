// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_proven_generated_concept_rules : a_validated_generation
{
    [Theory]
    [InlineData("GenerateValue.NanoId()", "RuleFor(value => value.Value).Matches(\"^[A-Za-z0-9_-]{21}$\");", "generate nanoid")]
    [InlineData("GenerateValue.Ulid()", "RuleFor(value => value.Value).Matches(\"^[0-7][0-9A-HJKMNP-TV-Z]{25}$\");", "generate ulid")]
    public void should_admit_proven_rules_and_their_mapping(string call, string rule, string line)
    {
        Generate((Analyzed.SlicePath, ValidatedSource(call, rule)));
        AssertStrategy(line);
        Result.Source.ShouldContain("matches");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V20);
    }

    [Theory]
    [InlineData(false, 19, false)]
    [InlineData(true, 19, false)]
    [InlineData(false, 20, true)]
    [InlineData(true, 20, true)]
    [InlineData(false, 0, true)]
    [InlineData(true, 0, true)]
    public void should_share_the_v20_cap_in_both_modes(bool authoring, int cap, bool admitted)
    {
        var version = cap == 19 ? SemanticVersion.V19 : cap == 20 ? SemanticVersion.V20 : (SemanticVersion?)null;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = version },
            (Analyzed.SlicePath, ValidatedSource("GenerateValue.NanoId()", "RuleFor(value => value.Value).NotEmpty();")));
        Result.Source.ShouldContain("not empty");
        Result.Source.Contains("generate nanoid", StringComparison.Ordinal).ShouldEqual(admitted);
        Result.Source.Contains("receipt AuthorId generated", StringComparison.Ordinal).ShouldEqual(admitted);
        if (!admitted)
        {
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable && diagnostic.Message.Contains("ESM v20", StringComparison.Ordinal)).ShouldBeTrue();
            Result.Source.ShouldNotContain("receipt = receipt");
        }
        AssertPartialDocument();
    }

    [Fact]
    void should_ignore_intentionally_excluded_named_rule_sets()
    {
        Generate((Analyzed.SlicePath, ValidatedSource("GenerateValue.NanoId()", "RuleSet(\"manual\", () => RuleFor(value => value.Value).Must(value => false));")));
        AssertStrategy("generate nanoid");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V15);
    }
}
