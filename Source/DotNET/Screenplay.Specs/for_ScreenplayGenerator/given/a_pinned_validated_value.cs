// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Supplies a literal NanoID fixture to a response-only command with a concept validator.
/// </summary>
public class a_pinned_validated_value : a_generated_document
{
    protected void GeneratePin(string pin, string rule, ScreenplayOptions? options = null)
    {
        var command = $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Authors.Registration;
            public record ReceiptId(string Value) : ConceptAs<string>(Value), IGeneratable<ReceiptId>
            {
                public static ReceiptId New() => new(GenerateValue.NanoId(64, "ab"));
            }
            public class ReceiptValidator : Cratis.Arc.Validation.ConceptValidator<ReceiptId>
            {
                public ReceiptValidator() { {{rule}} }
                static bool Approved(string value) => value.Length == 64;
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
                void Establish() => _scenario.PinGeneratedValue("receipt", new ReceiptId("{{pin}}"));
                async Task Because() => _result = await _scenario.Execute(new AllocateReceipt());
                [Fact] void should_return_the_receipt() => Assert.Equal(new ReceiptId("{{pin}}"), ((CommandResult<ReceiptId>)_result).Response);
            }
            """;
        Generate(
            options ?? new(),
            (Analyzed.SlicePath, command),
            ("Library/Testing/Scenario.cs", Testing),
            ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
            ("Library/Authors/Registration/when_allocating/and_a_receipt_is_pinned.cs", scenario));
    }
}
