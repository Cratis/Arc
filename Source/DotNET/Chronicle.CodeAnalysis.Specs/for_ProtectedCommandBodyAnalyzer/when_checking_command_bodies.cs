// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Independently readable source fragments are concatenated.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ProtectedCommandBodyAnalyzer;

public class when_checking_command_bodies
{
    const string Imports = "using Cratis.Arc.Commands; using Cratis.Arc.Chronicle.ReadModels; using Cratis.Arc.Chronicle.Aggregates; using Cratis.Chronicle.EventSequences; using Cratis.Chronicle.Transactions; ";

    [Fact]
    public Task should_report_a_raw_read_under_on() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(IReadModels models) { _ = await models.GetInstanceById<State>(new ReadModelKey(\"a\")); return new(); } }",
        Warning("ARCCHR0028").WithArguments("Change", "GetInstanceById", "not proven"));

    [Theory]
    [InlineData("")]
    [InlineData(", ReadProtection(ReadProtectionMode.Off)")]
    [InlineData(", ReadProtection(ReadProtectionMode.Measure)")]
    [InlineData(", Unprotected(\"rollout exception\")")]
    public Task should_not_report_raw_reads_outside_on(string declaration) => Verify(
        $"[Command{declaration}] public record Change(OwnerId Owner) {{ public async Task<Changed> Handle(IReadModels models) {{ _ = await models.GetInstanceById<State>(new ReadModelKey(\"a\")); return new(); }} }}");

    [Fact]
    public Task should_respect_an_assembly_rollout_and_command_exception() => AnalyzerVerifier<ProtectedCommandBodyAnalyzer>.VerifyAnalyzerAsync(
        Imports + DecisionAnalyzerSource.Definitions.Replace("[EventType]", "[assembly: ReadProtection(ReadProtectionMode.On)] [EventType]", StringComparison.Ordinal) + "[Command, ReadProtection(ReadProtectionMode.Off)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(IReadModels models) { _ = await models.GetInstanceById<State>(new ReadModelKey(\"a\")); return new(); } }");

    [Fact]
    public Task should_report_a_validator_raw_read() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle() => new(); } public class Validator : CommandValidator<Change> { public Validator(IReadModels models) { _ = models.GetInstanceById<State>(new ReadModelKey(\"a\")); } }",
        Warning("ARCCHR0028"));

    [Fact]
    public Task should_allow_a_method_advisory_body() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { [Unprotected(\"context\")] public async Task<Changed> Handle(IReadModels models) { _ = await models.GetInstanceById<State>(new ReadModelKey(\"a\")); return new(); } }");

    [Fact]
    public Task should_allow_command_aware_decision_reads() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public async Task<State> Provide(IDecisionReads reads) => (await reads.Get<State>(new ReadModelKey(\"a\"))).Instance!; public Changed Handle(State state) => new(); }");

    [Fact]
    public Task should_report_an_explicit_detached_acquisition() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(IDecisionReads reads) { _ = await reads.GetDetached<State>(new ReadModelKey(\"a\")); return new(); } }",
        Warning("ARCCHR0028").WithArguments("GetDetached"));

    [Fact]
    public Task should_report_an_explicit_read_allowance() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(IUnitOfWork unit) { using var allowance = unit.AllowReads(ReadAllowance.Advisory, \"context\"); return new(); } }",
        Warning("ARCCHR0028").WithArguments("AllowReads"));

    [Fact]
    public Task should_not_report_unrelated_apis_with_the_same_name() => Verify(
        "public class Local { public State GetDetached() => new(new OwnerId(Guid.NewGuid())); } [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Local reader) { _ = reader.GetDetached(); return new(); } }");

    [Fact]
    public Task should_report_a_read_after_commit_in_the_same_block() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(Account account, IDecisionReads reads) { await account.Commit(); _ = await reads.Get<State>(new ReadModelKey(\"a\")); return new(); } }",
        Warning("ARCCHR0035").WithArguments("later read or append"));

    [Fact]
    public Task should_report_a_transactional_append_after_commit() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public async Task<Changed> Handle(Account account, IEventLog log) { await account.Commit(); await log.Transactional.Append(Owner, new Changed()); return new(); } }",
        Warning("ARCCHR0035"));

    [Fact]
    public Task should_allow_commit_as_the_last_operation() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Task Handle(Account account) => account.Commit(); }");

    [Fact]
    public Task should_not_compare_offsets_across_mutually_exclusive_branches() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner, bool CommitNow) { public async Task<Changed> Handle(Account account, IDecisionReads reads) { if (CommitNow) { await account.Commit(); } else { _ = await reads.Get<State>(new ReadModelKey(\"a\")); } return new(); } }");

    [Fact]
    public Task should_report_commit_when_the_command_is_invoked_as_nested() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Inner(OwnerId Owner) { public Task Handle(Account account) => account.Commit(); } [Command, ReadProtection(ReadProtectionMode.On)] public record Outer(OwnerId Owner) { public async Task<Changed> Handle(ICommandPipeline pipeline) { await pipeline.Execute(new Inner(Owner)); return new(); } }",
        Warning("ARCCHR0035").WithArguments("nested"));

    [Fact]
    public Task should_not_treat_top_level_pipeline_execution_as_nested() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Task Handle(Account account) => account.Commit(); } public class Endpoint { public Task Run(ICommandPipeline pipeline) => pipeline.Execute(new Change(new OwnerId(Guid.NewGuid()))); }");

    static DiagnosticBuilder Warning(string id) => AnalyzerVerifier<ProtectedCommandBodyAnalyzer>.Diagnostic(id).WithSeverity(DiagnosticSeverity.Warning);
    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) =>
        AnalyzerVerifier<ProtectedCommandBodyAnalyzer>.VerifyAnalyzerAsync(Imports + DecisionAnalyzerSource.Definitions + source, diagnostics);
}
