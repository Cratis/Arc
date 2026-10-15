// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_v21_generation_pins : a_generated_document
{
    [Theory]
    [InlineData("string", "GenerateValue.Pattern(\"[AB]{20}\")", "\"AAAAAAAAAAAAAAAAAAAA\"", true)]
    [InlineData("string", "GenerateValue.Pattern(\"[AB]{20}\")", "\"invalid\"", false)]
    [InlineData("string", "GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Luhn)", "\"000000000000000000000\"", true)]
    [InlineData("string", "GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Luhn)", "\"000000000000000000001\"", false)]
    [InlineData("long", "GenerateValue.Integer(-9, 9)", "4L", true)]
    [InlineData("long", "GenerateValue.Integer(-9, 9)", "10L", false)]
    [InlineData("int", "(int)GenerateValue.Integer(-9, 9)", "4", true)]
    [InlineData("string", "string.Concat(\"prefix\", Guid.NewGuid().ToString())", "\"prefix-example\"", true)]
    public void should_check_concrete_fixtures_with_the_language_runner(string primitive, string call, string literal, bool valid)
    {
        var command = $$"""
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            namespace Library.Authors.Registration;
            public record ReceiptId({{primitive}} Value) : ConceptAs<{{primitive}}>(Value), IGeneratable<ReceiptId>
            {
                public static ReceiptId New() => new({{call}});
            }
            [Command] public record AllocateReceipt
            {
                public ReceiptId Handle([GeneratedValue] ReceiptId receipt) => receipt;
            }
            """;
        const string Testing = """
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            namespace Cratis.Arc.Testing.Commands;
            public class CommandScenario<TCommand>
            {
                public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
            }
            """;
        var scenario = $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Xunit;
            namespace Library.Authors.Registration.when_allocating;
            public class and_a_receipt_is_pinned
            {
                readonly CommandScenario<AllocateReceipt> _scenario = new();
                CommandResult _result = null!;
                void Establish() => _scenario.PinGeneratedValue("receipt", new ReceiptId({{literal}}));
                async Task Because() => _result = await _scenario.Execute(new AllocateReceipt());
                [Fact] void should_return_the_receipt() => Assert.Equal(new ReceiptId({{literal}}), ((CommandResult<ReceiptId>)_result).Response);
            }
            """;
        Generate(
            (Analyzed.SlicePath, command),
            ("Library/Testing/Scenario.cs", Testing),
            ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
            ("Library/Authors/Registration/when_allocating/and_a_receipt_is_pinned.cs", scenario));
        Assert.True(Result.Source.Contains("specification WhenAllocatingAndAReceiptIsPinned", StringComparison.Ordinal) == valid, $"{Result.Source}\n{string.Join('\n', Result.Diagnostics.Select(diagnostic => diagnostic.Message))}");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldEqual(!valid);
        AssertPartialDocument();
        if (valid)
        {
            Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
        }
    }
}
