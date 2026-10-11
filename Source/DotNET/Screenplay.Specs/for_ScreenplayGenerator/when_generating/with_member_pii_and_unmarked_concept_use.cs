// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_member_pii_and_unmarked_concept_use : given.a_compliance_document
{
    void Because() => GeneratePersonalData("EmailAddress", string.Empty);

    [Fact] void should_promote_the_concept() => Result.Source.ShouldContain("concept EmailAddress : String pii");
    [Fact] void should_name_the_concept_and_the_unmarked_use() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ConceptPiiPromotion).Message.ShouldEqual("Concept 'EmailAddress' is promoted to pii by member [PII] markings; the document also classifies these unmarked uses as pii: Library.Authors.Registration.EmailSet.Email");
    [Fact] void should_report_information() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ConceptPiiPromotion).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
