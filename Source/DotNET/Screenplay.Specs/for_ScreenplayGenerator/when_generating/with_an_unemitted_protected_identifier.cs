// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_an_unemitted_protected_identifier : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_the_true_personal_data_claim(bool authoring)
    {
        const string Source = """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.EventSequences;
            using Cratis.Chronicle.Keys;
            using Cratis.Concepts;

            namespace Library.Authors.Registration;

            [PII]
            public record EmailAddress(string Value) : ConceptAs<string>(Value);

            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record SetEmail([Key] EmailAddress Email, string OtherId)
            {
                public EventForEventSourceId Handle() => new(OtherId, new EmailSet(Email));
            }

            [Command]
            public record StoreEmail(EmailAddress Email)
            {
                public EmailStored Handle() => new(Email);
            }

            [EventType]
            public record EmailSet(EmailAddress Email);

            [EventType]
            public record EmailStored(EmailAddress Email);
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, Source));

        Result.Model.Slices.SelectMany(slice => slice.Commands).Single(command => command.Name == "SetEmail").Identifier.ShouldEqual("Email");
        Result.Source.ShouldNotContain(" identifier");
        Result.Source.ShouldNotContain("eventsource Account");
        Result.Source.ShouldContain("concept EmailAddress : String pii");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation).ShouldBeEmpty();
        Result.Diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotBind, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.SourceDidNotCompile, StringComparison.Ordinal)).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error && !ExpectedBindingDiagnostics.IsExpected(diagnostic, authoring)).ShouldBeEmpty();
    }
}
