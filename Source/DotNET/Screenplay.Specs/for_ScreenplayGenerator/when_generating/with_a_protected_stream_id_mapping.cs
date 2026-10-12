// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_a_protected_stream_id_mapping : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_the_annotation_and_name_both_retained_positions(bool authoring)
    {
        const string Source = """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Concepts;

            namespace Library.Authors.Registration;

            [PII]
            public record EmailAddress(string Value) : ConceptAs<string>(Value);

            [Command, EventSourceType("Account"), EventStreamType("Transactions"), EventStreamId("{Email}")]
            public record SetEmail([Key] string Id, EmailAddress Email)
            {
                public EmailSet Handle() => new(Email);
            }

            [EventType]
            public record EmailSet(EmailAddress Email);
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, Source));

        Result.Source.ShouldContain("streamId EmailAddress");
        Result.Source.ShouldContain("streamId = email");
        Result.Source.ShouldNotContain("concept EmailAddress : String pii");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation);
        diagnostic.Message.ShouldContain("stream id of 'Account.Transactions'");
        diagnostic.Message.ShouldContain("stream id route mapping of 'Account.Transactions' in command 'SetEmail'");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile || diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile || diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error && !ExpectedBindingDiagnostics.IsExpected(diagnostic, authoring)).ShouldBeEmpty();
    }
}
