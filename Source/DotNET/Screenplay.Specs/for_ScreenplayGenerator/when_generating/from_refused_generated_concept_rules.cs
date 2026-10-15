// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_refused_generated_concept_rules : a_validated_generation
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_a_refuted_strategy_without_dropping_its_rule(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
            (Analyzed.SlicePath, ValidatedSource("GenerateValue.NanoId()", "RuleFor(value => value.Value).Length(22);")));
        Result.Source.ShouldContain("length == 22");
        Result.Source.ShouldNotContain("generate nanoid");
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Source.ShouldNotContain("receipt = receipt");
        var refusal = Result.Diagnostics.First(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable && diagnostic.Message.Contains("PLAY0688", StringComparison.Ordinal));
        refusal.Message.ShouldContain("PLAY0688");
        refusal.Message.ShouldContain("witness");
        refusal.Location.ShouldNotBeNull();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData("RuleFor(value => value.Value).Must(value => value.Length == 21);")]
    [InlineData("RuleFor(value => value.Value).NotEmpty().When(value => value.Value.Length > 0);")]
    [InlineData("if (DateTime.UtcNow.Year > 2020) RuleFor(value => value.Value).NotEmpty();")]
    public void should_withhold_incompletely_recovered_validation(string registration)
    {
        Generate((Analyzed.SlicePath, ValidatedSource("GenerateValue.NanoId()", registration)));
        Result.Source.ShouldNotContain("generate nanoid");
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable && diagnostic.Message.Contains("complete executable validation contract", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
