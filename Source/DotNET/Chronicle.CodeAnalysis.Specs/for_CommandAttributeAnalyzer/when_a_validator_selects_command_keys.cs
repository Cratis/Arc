// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_validator_selects_command_keys
{
    [Fact]
    public Task should_resolve_both_keys_on_the_command_through_a_generic_base() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync("""
        using Cratis.Arc.Commands;
        public record Transfer(string From, string To);
        public record Balance(string Id);
        public abstract class Validator<T> : CommandValidator<T> { }
        public class TransferValidator(
            [LookUpBy(nameof(Transfer.From))] Balance from,
            [LookUpBy(nameof(Transfer.To))] Balance to) : Validator<Transfer> { }
        """);

    [Fact]
    public Task should_offer_nameof_the_command_property() => CodeFixVerifier<CommandAttributeAnalyzer, UseNameofForLookUpByCodeFixProvider>.VerifyCodeFixAsync(
        "using Cratis.Arc.Commands; public record Transfer(string From); public record Balance(string Id); public class Validator([LookUpBy(\"Fro\")] Balance model) : CommandValidator<Transfer> { }",
        "using Cratis.Arc.Commands; public record Transfer(string From); public record Balance(string Id); public class Validator([LookUpBy(nameof(global::Transfer.From))] Balance model) : CommandValidator<Transfer> { }",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "Fro"));

    [Fact]
    public Task should_not_accept_a_property_only_on_the_validator() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        """
        using Cratis.Arc.Commands;
        public record Transfer(string From);
        public record Balance(string Id);
        public class TransferValidator([LookUpBy(nameof(TransferValidator.Other))] Balance from) : CommandValidator<Transfer>
        {
            public string Other { get; } = string.Empty;
        }
        """,
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "command 'Transfer'", "Other"));
}
