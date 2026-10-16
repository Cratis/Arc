// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_checksum_validation : a_validated_generation
{
    [Theory]
    [InlineData("Luhn", "luhn")]
    [InlineData("Mod11", "mod11")]
    [InlineData("Mod97", "mod97")]
    public void should_prove_pattern_and_checksum_together(string algorithm, string name)
    {
        Generate((Analyzed.SlicePath, ValidatedSource($"GenerateValue.Pattern(\"[0-9]{{20}}\", checksum: Checksum.{algorithm})",
            $"RuleFor(value => value.Value).Must(v => GenerateValue.HasValidChecksum(v, Checksum.{algorithm}));")));
        AssertStrategy($"generate pattern \"[0-9]{{20}}\" checksum {name}");
        Result.Source.ShouldContain($"checksum {name}");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    }

    [Theory]
    [InlineData("RuleFor(value => value.Value).Must(v => GenerateValue.HasValidChecksum(v, Checksum.None));")]
    [InlineData("RuleFor(value => value.Value).Must(v => GenerateValue.HasValidChecksum(v, Checksum.Luhn)).When(value => value.Value.Length > 0);")]
    public void should_not_claim_unsupported_checksum_predicates_are_proven(string rule)
    {
        Generate((Analyzed.SlicePath, ValidatedSource("GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Luhn)", rule)));
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData(false, 20)]
    [InlineData(true, 20)]
    [InlineData(false, 21)]
    [InlineData(true, 21)]
    public void should_keep_the_validator_below_the_v21_cap(bool authoring, int cap)
    {
        var source = ValidatedSource("GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Luhn)",
            "RuleFor(value => value.Value).Must(v => GenerateValue.HasValidChecksum(v, Checksum.Luhn));");
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = cap == 20 ? SemanticVersion.V20 : SemanticVersion.V21 },
            (Analyzed.SlicePath, source));
        Result.Source.Contains("generate pattern", StringComparison.Ordinal).ShouldEqual(cap == 21);
        Result.Source.Contains("receipt AuthorId generated", StringComparison.Ordinal).ShouldEqual(cap == 21);
        if (cap == 20)
        {
            Result.Source.ShouldContain("rule AuthorIdValidator");
            Result.Source.ShouldNotContain("checksum luhn");
            Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("checksum validation requires ESM v21", StringComparison.Ordinal)).ShouldBeTrue();
        }
        AssertPartialDocument();
    }
}
