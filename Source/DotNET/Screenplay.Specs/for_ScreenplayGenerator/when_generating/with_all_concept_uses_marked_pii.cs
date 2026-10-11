// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_all_concept_uses_marked_pii : given.a_compliance_document
{
    void Because() => GeneratePersonalData("EmailAddress", "[property: PII]");

    [Fact] void should_promote_the_concept() => Result.Source.ShouldContain("concept EmailAddress : String pii");
    [Fact] void should_not_report_widened_classification() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ConceptPiiPromotion).ShouldBeEmpty();
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
