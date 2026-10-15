// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Independently readable source fragments are concatenated.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_read_protection_is_declared
{
    const string Imports = "using Cratis.Arc.Commands; using Cratis.Arc.Chronicle.ReadModels; using Cratis.Chronicle.EventSequences; ";

    [Theory]
    [InlineData("Measure")]
    [InlineData("On")]
    public Task should_not_advise_that_an_on_or_measure_parameter_is_an_off_read(string mode) => Verify(
        $"[Command, ReadProtection(ReadProtectionMode.{mode})] public record Change(OwnerId Owner) {{ public Changed Handle(State state) => new(); }}");

    [Fact]
    public Task should_recommend_on_and_validator_constructor_in_off() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.Off)] public record Change(OwnerId Owner) { public Changed Handle() => new(); } public class Validator(State state) : CommandValidator<Change>;",
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info).WithArguments("CommandValidator constructor", "only a hint"));

    [Fact]
    public Task should_warn_about_immediate_appends_under_on_even_without_a_decision_parameter() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(IEventLog log) { await log.Append(Owner, new Changed()); return new(); } }",
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0012").WithSeverity(DiagnosticSeverity.Warning));

    [Fact]
    public Task should_not_treat_an_advisory_body_as_permission_to_append() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { [Unprotected(\"context\")] public async Task<Changed> Handle(IEventLog log) { await log.Append(Owner, new Changed()); return new(); } }",
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0012").WithSeverity(DiagnosticSeverity.Warning));

    [Fact]
    public Task should_allow_a_transactional_append_under_on() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(IEventLog log) { await log.Transactional.Append(Owner, new Changed()); return new(); } }");

    [Fact]
    public Task should_not_warn_about_immediate_appends_in_an_ordinary_off_command() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.Off)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(IEventLog log) { await log.Append(Owner, new Changed()); return new(); } }");

    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) =>
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Imports + DecisionAnalyzerSource.Definitions + source, diagnostics);
}
