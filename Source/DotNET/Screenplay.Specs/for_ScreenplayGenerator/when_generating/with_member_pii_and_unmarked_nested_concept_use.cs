// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_member_pii_and_unmarked_nested_concept_use : given.a_compliance_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record EmailAddress(string Value) : ConceptAs<string>(Value);
        public record Contact(EmailAddress Email);

        [Command]
        public record SetEmail([property: PII] EmailAddress Email, Contact Contact)
        {
            public EmailSet Handle() => new(Email, Contact);
        }

        [EventType]
        public record EmailSet([PII] EmailAddress Email, Contact Contact);
        """));

    [Fact] void should_promote_the_concept() => Result.Source.ShouldContain("concept EmailAddress : String pii");
    [Fact] void should_report_the_unmarked_nested_use_once() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ConceptPiiPromotion).Message.ShouldEqual("Concept 'EmailAddress' is promoted to pii by member [PII] markings; the document also classifies these unmarked uses as pii: Library.Authors.Registration.Contact.Email");
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
