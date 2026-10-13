// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_protected_default_uuid_generation : a_compliance_document
{
    [Theory]
    [InlineData("[PII]")]
    [InlineData("[Encrypted, NotAudited]")]
    public void should_preserve_default_uuid_generation_despite_compliance_markers(string attributes)
    {
        Generate((Analyzed.SlicePath, $$"""
            using System;
            using Cratis.Arc.Chronicle.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Compliance.GDPR;
            using Cratis.Chronicle.ProtectedValues;
            using Cratis.Concepts;
            namespace Library.Authors.Registration;
            {{attributes}}
            public record ReceiptId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<ReceiptId>
            {
                public static ReceiptId New() => new(GenerateValue.Uuid());
            }
            [Command] public record AllocateReceipt
            {
                public ReceiptId Handle([GeneratedValue] ReceiptId receipt) => receipt;
            }
            """));
        Result.Source.ShouldContain("receipt ReceiptId generated");
        Result.Source.ShouldNotContain("generate uuid");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeEmpty();
        AssertComplianceDocument();
    }
}
