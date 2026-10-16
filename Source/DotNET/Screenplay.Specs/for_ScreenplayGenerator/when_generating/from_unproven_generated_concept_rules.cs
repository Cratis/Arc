// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unproven_generated_concept_rules : a_validated_generation
{
    void Because() => Generate((Analyzed.SlicePath, ValidatedSource("GenerateValue.NanoId(64, \"ab\")", "RuleFor(value => value.Value).Matches(\"^(?!a{63}b$)[ab]{64}$\");")));

    [Fact] void should_withhold_the_nonportable_rule() => Result.Source.ShouldNotContain("matches");
    [Fact] void should_not_weaken_generation_by_removing_its_rule() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_name_the_concept_and_refused_construct() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule && diagnostic.Message.Contains("AuthorId.Value", StringComparison.Ordinal) && diagnostic.Message.Contains("lookaround", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_mislabel_the_warning_as_a_defect() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
    [Fact] void should_round_trip_with_only_the_existing_handler_admission_limit() => AssertPartialDocument();
}
