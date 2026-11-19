// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_finding_decision_scopes : Specification
{
    [Fact]
    public Task should_find_the_command_through_an_intermediate_validator_base() =>
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(
            $$"""
            {{DecisionAnalyzerSource.Definitions}}
            [Command] public record Change { public Changed Handle() => new(); }
            public abstract class BaseValidator : Cratis.Arc.Commands.CommandValidator<Change>;
            public class Validator(State state) : BaseValidator;
            """,
            AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("Change", "State", "CommandValidator constructor"));

    [Theory]
    [InlineData("public record Change { public Changed Handle(State state) => new(); }")]
    [InlineData("[Command] public record Change { public Changed Handle() => new(); public Changed Helper(State state) => new(); }")]
    [InlineData("public record Change { public Changed Handle() => new(); } public class Validator(State state) : Cratis.Arc.Commands.CommandValidator<Change>;")]
    [InlineData("[Command] public record Change { public int Handle() => 0; } public class Validator(State state) : Cratis.Arc.Commands.CommandValidator<Change>;")]
    [InlineData("public class CommandValidator<T>; [Command] public record Change { public Changed Handle() => new(); } public class Validator(State state) : CommandValidator<Change>;")]
    [InlineData("public class CommandValidator; public class Validator(State state) : CommandValidator;")]
    public Task should_ignore_types_and_methods_outside_a_decision(string declaration) =>
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync($$"""
            {{DecisionAnalyzerSource.Definitions}}
            {{declaration}}
            """);
}
