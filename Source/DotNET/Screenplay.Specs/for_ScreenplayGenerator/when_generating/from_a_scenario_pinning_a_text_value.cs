// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_scenario_pinning_a_text_value : a_generated_document
{
    const string Testing = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        namespace Cratis.Arc.Testing.Commands;
        public class CommandScenario<TCommand>
        {
            public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
        }
        """;

    [Theory]
    [InlineData("GenerateValue.NanoId()", "ABCDEFGHIJKLMNOPQRSTU")]
    [InlineData("GenerateValue.Ulid()", "01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("GenerateValue.TypeId(\"user\")", "user_0000000000e008000000000000")]
    [InlineData("GenerateValue.Bytes(8, ValueEncoding.Hex)", "0123456789abcdef")]
    [InlineData("GenerateValue.Bytes(8)", "AAAAAAAAAAA")]
    [InlineData("GenerateValue.Bytes(8, ValueEncoding.Base32)", "AAAAAAAAAAAAA")]
    public void should_emit_and_execute_a_literal_text_fixture(string call, string pin)
    {
        GeneratePinned(call, pin);
        Result.Source.ShouldContain($"generated receipt = \"{pin}\"");
        Result.Source.ShouldContain($"then returns \"{pin}\"");
        AssertDocument();
        Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("GenerateValue.NanoId()", "short")]
    [InlineData("GenerateValue.Ulid()", "01arz3ndektsv4rrffq69g5fav")]
    [InlineData("GenerateValue.Ulid()", "81ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("GenerateValue.TypeId(\"user\")", "wrong_0000000000e008000000000000")]
    [InlineData("GenerateValue.TypeId(\"user\")", "user_00000000000000000000000000")]
    [InlineData("GenerateValue.Bytes(8, ValueEncoding.Hex)", "0123456789ABCDEF")]
    [InlineData("GenerateValue.Bytes(8)", "AAAAAAAAAAB")]
    [InlineData("GenerateValue.Bytes(8, ValueEncoding.Base32)", "AAAAAAAAAAAAB")]
    public void should_withhold_a_fixture_outside_the_strategy_shape(string call, string pin)
    {
        GeneratePinned(call, pin);
        Result.Source.ShouldContain("receipt ReceiptId generated");
        Result.Source.ShouldNotContain("specification WhenAllocatingAndAReceiptIsPinned");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("literal");
        AssertDocument();
    }

    void GeneratePinned(string call, string pin)
    {
        var command = $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            namespace Library.Authors.Registration;
            public record ReceiptId(string Value) : ConceptAs<string>(Value), IGeneratable<ReceiptId>
            {
                public static ReceiptId New() => new({{call}});
            }
            [Command] public record AllocateReceipt
            {
                public ReceiptId Handle([GeneratedValue] ReceiptId receipt) => receipt;
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
                void Establish() => _scenario.PinGeneratedValue("receipt", new ReceiptId("{{pin}}"));
                async Task Because() => _result = await _scenario.Execute(new AllocateReceipt());
                [Fact] void should_return_the_receipt() => Assert.Equal(new ReceiptId("{{pin}}"), ((CommandResult<ReceiptId>)_result).Response);
            }
            """;
        Generate(
            (Analyzed.SlicePath, command),
            ("Library/Testing/Scenario.cs", Testing),
            ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
            ("Library/Authors/Registration/when_allocating/and_a_receipt_is_pinned.cs", scenario));
    }
}
