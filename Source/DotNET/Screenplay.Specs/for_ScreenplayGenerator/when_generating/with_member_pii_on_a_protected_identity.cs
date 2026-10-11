// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_member_pii_on_a_protected_identity : given.a_compliance_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record EmailAddress(string Value) : ConceptAs<string>(Value);

        [Command]
        public record SetEmail([property: PII, Key] EmailAddress Email)
        {
            public EmailSet Handle() => new(Email);
        }

        [EventType]
        public record EmailSet(EmailAddress Email);
        """));

    [Fact] void should_withhold_the_protected_identity_marker() => Result.Source.ShouldNotContain("concept EmailAddress : String pii");
    [Fact] void should_report_the_withheld_marker() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_not_claim_the_document_promotes_personal_data() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ConceptPiiPromotion).ShouldBeEmpty();
    [Fact] void should_compile_and_round_trip() => AssertDocument();
}
