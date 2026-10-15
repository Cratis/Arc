// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_nongeneratable_checksum_concept : a_generated_document
{
    [Theory]
    [InlineData(false, 20)]
    [InlineData(true, 20)]
    [InlineData(false, 21)]
    [InlineData(true, 21)]
    public void should_preserve_checksum_validation_without_a_generation_factory(bool authoring, int cap)
    {
        const string Source = """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Receipts.Accepting;
            public record Receipt(string Value) : ConceptAs<string>(Value);
            public class ReceiptValidator : Cratis.Arc.Validation.ConceptValidator<Receipt>
            {
                public ReceiptValidator() => RuleFor(value => value.Value).Must(v => GenerateValue.HasValidChecksum(v, Checksum.Luhn));
            }
            [Command] public record AcceptReceipt(Receipt Receipt)
            {
                public void Handle() { }
            }
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = cap == 20 ? SemanticVersion.V20 : SemanticVersion.V21 },
            (Analyzed.SlicePath, Source));
        Result.Source.Contains("checksum luhn", StringComparison.Ordinal).ShouldEqual(cap == 21);
        if (cap == 20)
        {
            Result.Source.ShouldContain("rule ReceiptValidator");
            Result.Source.ShouldContain("file Feature/Slice/Slice.cs");
            Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("checksum validation requires ESM v21", StringComparison.Ordinal)).ShouldBeTrue();
        }
        Result.Source.ShouldNotContain("generate ");
        AssertPartialDocument();
    }
}
