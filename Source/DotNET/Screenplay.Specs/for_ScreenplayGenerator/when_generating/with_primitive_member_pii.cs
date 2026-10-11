// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_primitive_member_pii : given.a_compliance_document
{
    void Because() => GeneratePersonalData("string", string.Empty);

    [Fact] void should_not_invent_a_concept() => Result.Model.Concepts.ShouldBeEmpty();
    [Fact] void should_not_state_personal_data() => Result.Source.ShouldNotContain("pii");
    [Fact] void should_name_the_member_and_explain_the_loss() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.MemberPiiNotRepresentable).Message.ShouldContain("Member 'Library.Authors.Registration.SetEmail.Email' has [PII] on a non-concept type; Screenplay classifies only concepts");
    [Fact] void should_suggest_a_concept() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.MemberPiiNotRepresentable).Message.ShouldContain("Use a personal-data concept");
    [Fact] void should_report_information() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.MemberPiiNotRepresentable).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
