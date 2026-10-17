// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unproven_generated_concept_rules : a_validated_generation
{
    void Because() => Generate((Analyzed.SlicePath, ValidatedSource("GenerateValue.NanoId(64, \"ab\")", "RuleFor(value => value.Value).Matches(\"^(aa|bb|ab|ba){32}$\");")));

    [Fact] void should_keep_the_nonopaque_rule() => Result.Source.ShouldContain("matches \"^(aa|bb|ab|ba){32}$\"");
    [Fact] void should_keep_generation() => Result.Source.ShouldContain("receipt AuthorId generated");
    [Fact] void should_select_v20() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V20);
    [Fact] void should_surface_the_language_warning_as_information() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprovenGeneratedConceptRule).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_name_the_language_warning() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprovenGeneratedConceptRule).Message.ShouldContain("PLAY0689");
    [Fact] void should_not_mislabel_the_warning_as_a_defect() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
