// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_operational_secrets : a_generated_document
{
    [Theory]
    [InlineData("[Encrypted, NotAudited]", "secret", false)]
    [InlineData("[PII, Encrypted, NotAudited]", "pii secret", false)]
    [InlineData("[Encrypted]", "", true)]
    [InlineData("[NotAudited]", "", true)]
    [InlineData("[PII, Encrypted]", "pii", true)]
    [InlineData("[PII, NotAudited]", "pii", true)]
    public void should_map_only_the_complete_secret_contract(string attributes, string annotation, bool partial)
    {
        GenerateSecret(attributes, string.Empty, false);

        Result.Source.ShouldContain($"concept Secret : String{(annotation.Length == 0 ? string.Empty : $" {annotation}")}");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PartialSecretMarking).ShouldEqual(partial);
        AssertCompiles();
    }

    [Theory]
    [InlineData("[Encrypted, NotAudited]")]
    [InlineData("[property: Encrypted, NotAudited]")]
    public void should_not_promote_positional_parameter_and_property_markings_to_a_concept_contract(string attributes)
    {
        GenerateSecret(string.Empty, attributes, false);

        Result.Source.ShouldContain("concept Secret : String");
        Result.Source.ShouldNotContain("concept Secret : String secret");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PartialSecretMarking).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        AssertCompiles();
    }

    [Fact]
    public void should_not_promote_one_suppressed_command_to_a_concept_contract()
    {
        Generate((Analyzed.SlicePath, """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.ProtectedValues;
            using Cratis.Concepts;

            namespace Library.Authors.Registration;

            [Encrypted]
            public record Secret(string Value) : ConceptAs<string>(Value);

            [Command]
            public record SetSecret([property: NotAudited] Secret Secret)
            {
                public SecretSet Handle() => new(Secret);
            }

            [Command]
            public record ReplaceSecret(Secret Secret)
            {
                public SecretSet Handle() => new(Secret);
            }

            [EventType]
            public record SecretSet(Secret Secret);
            """));

        Result.Source.ShouldNotContain("concept Secret : String secret");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PartialSecretMarking).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        AssertCompiles();
    }

    [Fact]
    public void should_not_report_primitive_member_markings_as_partial_concepts()
    {
        Generate((Analyzed.SlicePath, """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.ProtectedValues;

            namespace Library.Authors.Registration;

            [Command, NotAudited]
            public record SetSecret([property: Encrypted, NotAudited] string Secret, string OldPassword)
            {
                public SecretSet Handle() => new(Secret);
            }

            [EventType]
            public record SecretSet(string Secret);
            """));

        Result.Source.ShouldNotContain("concept Secret : String secret");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PartialSecretMarking).ShouldBeEmpty();
        AssertCompiles();
    }

    [Theory]
    [InlineData("[Encrypted, NotAudited]")]
    [InlineData("[PII, Encrypted, NotAudited]")]
    [InlineData("[PII]")]
    public void should_withhold_protected_identity_annotations(string attributes)
    {
        GenerateSecret(attributes, string.Empty, true);

        Result.Source.ShouldContain("secret Secret identifier");
        Result.Source.ShouldNotContain("concept Secret : String pii");
        Result.Source.ShouldNotContain("concept Secret : String secret");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        AssertDocument();
    }

    [Theory]
    [InlineData("[Encrypted, NotAudited]")]
    [InlineData("[PII]")]
    public void should_withhold_compliance_annotations_from_stream_ids(string attributes)
    {
        var source = $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Chronicle.ProtectedValues;

            namespace Library.Authors.Registration;

            {{attributes}}
            public record Secret(string Value) : EventStreamId(Value);

            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record SetSecret([Key] string Id, Secret Secret) : ICanProvideEventStreamId
            {
                public EventStreamId GetEventStreamId() => Secret;
                public SecretSet Handle() => new(Secret);
            }

            [EventType]
            public record SecretSet(Secret Secret);
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = true }, (Analyzed.SlicePath, source));

        Result.Source.ShouldContain("streamId Secret");
        Result.Source.ShouldContain("streamId = secret");
        Result.Source.ShouldNotContain("concept Secret : String pii");
        Result.Source.ShouldNotContain("concept Secret : String secret");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation).ShouldBeTrue();
        AssertCompiles();
    }

    void GenerateSecret(string conceptAttributes, string memberAttributes, bool identifier) => Generate(
        (Analyzed.SlicePath, $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Chronicle.ProtectedValues;
            using Cratis.Concepts;

            namespace Library.Authors.Registration;

            {{conceptAttributes}}
            public record Secret(string Value) : ConceptAs<string>(Value);

            [Command]
            public record SetSecret({{(identifier ? "[property: Key]" : string.Empty)}} {{memberAttributes}} Secret Secret)
            {
                public SecretSet Handle() => new(Secret);
            }

            [EventType]
            public record SecretSet(Secret Secret);
            """));

    void AssertCompiles()
    {
        Result.Diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.SourceDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotCompile, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.DocumentDidNotBind, StringComparison.Ordinal)).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
