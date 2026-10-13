// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Declares a pinned UUID response and optional independent generation declarations.
/// </summary>
public class a_pinned_uuid_value : a_generated_document
{
    protected const string NanoId = """
        public record Code(string Value) : ConceptAs<string>(Value), IGeneratable<Code>
        {
            public static Code New() => new(GenerateValue.NanoId());
        }
        [Command] public record UseCode(Code Code)
        {
            public void Handle() { }
        }
        """;

    protected const string CodeRules = """
        public class CodeValidator : Cratis.Arc.Validation.ConceptValidator<Code>
        {
            public CodeValidator() { RuleFor(value => value.Value).NotEmpty(); }
        }
        """;

    protected void GeneratePin(string pin, ScreenplayOptions options, string declarations = "", string factory = "GenerateValue.Uuid()")
    {
        var command = $$"""
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Authors.Registration;
            public record ReceiptId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<ReceiptId>
            {
                public ReceiptId(string value) : this(Guid.Parse(value)) { }
                public static ReceiptId New() => new({{factory}});
            }
            [Command] public record AllocateReceipt
            {
                public ReceiptId Handle([GeneratedValue] ReceiptId receipt) => receipt;
            }
            {{declarations}}
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
            using System;
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
                [Fact] void should_return_the_receipt() => Assert.Equal(new ReceiptId(Guid.Parse("{{pin}}")), ((CommandResult<ReceiptId>)_result).Response);
            }
            """;
        Generate(
            options,
            (Analyzed.SlicePath, command),
            ("Library/Testing/Scenario.cs", Testing),
            ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
            ("Library/Authors/Registration/when_allocating/and_a_receipt_is_pinned.cs", scenario));
    }
}
