// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Builds checksum-dependent and unrelated command scenarios with the same validation contract.
/// </summary>
public class a_checksum_scenario : a_generated_document
{
    protected const string ReceiptScenarioName = "WhenAcceptingAndAReceiptIsSupplied";
    protected const string PlainScenarioName = "WhenEchoingAndAPlainValueIsSupplied";
    const string ReceiptValue = "000000000000000000000";
    const string Testing = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        namespace Cratis.Arc.Testing.Commands;
        public class CommandScenario<TCommand>
        {
            public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
        }
        """;
    const string PlainScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Testing.Commands;
        using Xunit;
        namespace Library.Receipts.Accepting.when_echoing;
        public class and_a_plain_value_is_supplied
        {
            readonly CommandScenario<EchoPlain> _scenario = new();
            CommandResult _result = null!;
            async Task Because() => _result = await _scenario.Execute(new EchoPlain("ordinary"));
            [Fact] void should_echo_the_value() => Assert.Equal("ordinary", ((CommandResult<string>)_result).Response);
        }
        """;

    protected void GenerateChecksumScenario(bool generated, ScreenplayOptions options, string pin = ReceiptValue) => Generate(
        options,
        (Analyzed.SlicePath, Source(generated)),
        ("Library/Testing/Scenario.cs", Testing),
        ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
        ("Library/Receipts/Accepting/when_accepting/and_a_receipt_is_supplied.cs", ReceiptScenario(generated, pin)),
        ("Library/Receipts/Accepting/when_echoing/and_a_plain_value_is_supplied.cs", PlainScenario));

    static string Source(bool generated)
    {
        var contract = generated ? ", IGeneratable<Receipt>" : string.Empty;
        var factory = generated ? "public static Receipt New() => new(GenerateValue.Pattern(\"[0-9]{20}\", checksum: Checksum.Luhn));" : string.Empty;
        var command = generated ? "[Command] public record AcceptReceipt { public Receipt Handle([GeneratedValue] Receipt receipt) => receipt; }"
            : "[Command] public record AcceptReceipt(Receipt Receipt) { public Receipt Handle() => Receipt; }";

        return $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Receipts.Accepting;
            public record Receipt(string Value) : ConceptAs<string>(Value){{contract}}
            {
                public static implicit operator Receipt(string value) => new(value);
                {{factory}}
            }
            public class ReceiptValidator : Cratis.Arc.Validation.ConceptValidator<Receipt>
            {
                public ReceiptValidator() => RuleFor(value => value.Value).Must(v => GenerateValue.HasValidChecksum(v, Checksum.Luhn));
            }
            {{command}}
            [Command] public record EchoPlain(string Name) { public string Handle() => Name; }
            """;
    }

    static string ReceiptScenario(bool generated, string value)
    {
        var pin = generated ? $"_scenario.PinGeneratedValue(\"receipt\", new Receipt(\"{value}\"));" : string.Empty;
        var input = generated ? string.Empty : $"\"{value}\"";

        return $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Xunit;
            namespace Library.Receipts.Accepting.when_accepting;
            public class and_a_receipt_is_supplied
            {
                readonly CommandScenario<AcceptReceipt> _scenario = new();
                CommandResult _result = null!;
                void Establish() { {{pin}} }
                async Task Because() => _result = await _scenario.Execute(new AcceptReceipt({{input}}));
                [Fact] void should_return_the_receipt() => Assert.Equal(new Receipt("{{value}}"), ((CommandResult<Receipt>)_result).Response);
            }
            """;
    }
}
