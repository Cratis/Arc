// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_same_named_concepts_and_different_compliance : a_compliance_document
{
    [Theory]
    [InlineData(false, "[PII]", "", "pii")]
    [InlineData(true, "[PII]", "", "pii")]
    [InlineData(false, "", "[PII]", "")]
    [InlineData(true, "", "[PII]", "")]
    [InlineData(false, "[Encrypted, NotAudited]", "", "secret")]
    [InlineData(true, "[Encrypted, NotAudited]", "", "secret")]
    [InlineData(false, "", "[Encrypted, NotAudited]", "")]
    [InlineData(true, "", "[Encrypted, NotAudited]", "")]
    [InlineData(false, "[Encrypted(EncryptionScope.Namespace), NotAudited]", "[Encrypted(EncryptionScope.Global), NotAudited]", "secret")]
    [InlineData(true, "[Encrypted(EncryptionScope.Namespace), NotAudited]", "[Encrypted(EncryptionScope.Global), NotAudited]", "secret")]
    public void should_report_the_conflict_and_keep_the_first_declaration(bool authoring, string first, string second, string annotation)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
            ("Library/Authors/First/First.cs", Source("First", first)),
            ("Library/Authors/Second/Second.cs", Source("Second", second)));

        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.AmbiguousConceptName);
        diagnostic.Message.ShouldContain("Library.Authors.Second.ContactValue");
        Result.Model.Concepts.Count(concept => concept.Name == "ContactValue").ShouldEqual(1);
        Result.Model.Concepts.Single(concept => concept.Name == "ContactValue").IsPii.ShouldEqual(annotation == "pii");
        Result.Model.Concepts.Single(concept => concept.Name == "ContactValue").IsSensitive.ShouldEqual(annotation == "secret");
        Result.Source.ShouldContain($"concept ContactValue : String{(annotation.Length == 0 ? string.Empty : $" {annotation}")}");
        if (first.Contains("Namespace", StringComparison.Ordinal))
        {
            Result.Model.Concepts.Single(concept => concept.Name == "ContactValue").EncryptionScope.ShouldEqual("namespace");
            Result.Source.ShouldNotContain("scope global");
        }

        AssertComplianceDocument();
    }

    static string Source(string slice, string attributes) => $$"""
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.ProtectedValues;
        using Cratis.Concepts;

        namespace Library.Authors.{{slice}};

        {{attributes}}
        public record ContactValue(string Value) : ConceptAs<string>(Value);

        [Command]
        public record Set{{slice}}Contact(ContactValue Contact)
        {
            public {{slice}}ContactSet Handle() => new(Contact);
        }

        [EventType]
        public record {{slice}}ContactSet(ContactValue Contact);
        """;
}
