// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_an_emitted_protected_identifier : a_generated_document
{
    [Theory]
    [InlineData(false, "[PII]")]
    [InlineData(true, "[PII]")]
    [InlineData(false, "[Encrypted(EncryptionScope.Namespace), NotAudited]")]
    [InlineData(true, "[Encrypted(EncryptionScope.Namespace), NotAudited]")]
    public void should_name_the_retained_identity_and_withhold_only_its_compliance_claim(bool authoring, string attributes)
    {
        var source = $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Chronicle.ProtectedValues;
            using Cratis.Concepts;

            namespace Library.Authors.Registration;

            {{attributes}}
            public record ContactId(string Value) : ConceptAs<string>(Value);

            [Command]
            public record SetContact([Key] ContactId Id)
            {
                public ContactSet Handle() => new(Id);
            }

            [EventType]
            public record ContactSet(ContactId Id);
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));

        Result.Source.ShouldContain("id ContactId identifier");
        Result.Source.ShouldNotContain("concept ContactId : String pii");
        Result.Source.ShouldNotContain("concept ContactId : String secret");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation).Message.ShouldContain("identifier of command 'SetContact'");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EncryptionScopeNotRepresentable).ShouldBeEmpty();
        AssertDocument();
    }
}
