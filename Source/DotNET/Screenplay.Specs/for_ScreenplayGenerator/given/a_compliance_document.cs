// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_compliance_document : a_generated_document
{
    protected void GenerateSecret(string scope, bool authoring = false, bool pii = false)
    {
        var source = $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.ProtectedValues;
            using Cratis.Concepts;

            namespace Library.Authors.Registration;

            [Encrypted(EncryptionScope.{{scope}}), NotAudited]
            {{(pii ? "[PII]" : string.Empty)}}
            public record ApiKey(string Value) : ConceptAs<string>(Value);

            [Command]
            public record SetApiKey(ApiKey Key)
            {
                public ApiKeySet Handle() => new(Key);
            }

            [EventType]
            public record ApiKeySet(ApiKey Key);
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
    }

    protected void GeneratePersonalData(string valueType, string eventMarking, string conceptMarking = "") => Generate(
        (Analyzed.SlicePath, $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.Events;
            using Cratis.Concepts;

            namespace Library.Authors.Registration;

            {{conceptMarking}}
            public record EmailAddress(string Value) : ConceptAs<string>(Value);

            [Command]
            public record SetEmail([PII] {{valueType}} Email)
            {
                public EmailSet Handle() => new(Email);
            }

            [EventType]
            public record EmailSet({{eventMarking}} {{valueType}} Email);
            """));

    protected void AssertComplianceDocument()
    {
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile || diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile || diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
