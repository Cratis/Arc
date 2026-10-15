// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Independently readable source fragments are concatenated.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionParameterAnalyzer;

public class when_checking_protected_parameters
{
    const string Imports = "using Cratis.Arc.Commands; using Cratis.Arc.Chronicle.ReadModels; using Cratis.Arc.Chronicle.Aggregates; using Cratis.Chronicle.EventSequences; ";
    const string Reducer = "public record Balance(string Id); public class Fold : IReducerFor<Balance> { public Balance Reduce(Changed e, Balance? current) => new(\"a\"); }";

    [Theory]
    [InlineData("Measure")]
    [InlineData("On")]
    public Task should_refuse_event_property_keys(string mode) => Verify(
        "[FromEvent<Changed>(key: \"Owner\")] public record Selected(string Id);" +
        $"[Command, ReadProtection(ReadProtectionMode.{mode})] public record Change(OwnerId Owner) {{ public Changed Handle(Selected selected) => new(); }}",
        Warning("ARCCHR0024").WithArguments("selected", "event property", "constraint"));

    [Theory]
    [InlineData("")]
    [InlineData(", ReadProtection(ReadProtectionMode.Off)")]
    public Task should_not_report_refused_shapes_under_off(string declaration) => Verify(
        "[FromEvent<Changed>(key: \"Owner\")] public record Selected(int Id);" +
        $"[Command{declaration}] public record Change(OwnerId Owner) {{ public Changed Handle(Selected selected) => new(); }}");

    [Theory]
    [InlineData("[Unprotected(\"context\")] Selected selected")]
    [InlineData("Selected selected")]
    public Task should_honor_parameter_and_model_advisories(string parameter) => Verify(
        "[Unprotected(\"context\"), FromEvent<Changed>(key: \"Owner\")] public record Selected(int Id);" +
        $"[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) {{ public Changed Handle({parameter}) => new(); }}");

    [Fact]
    public Task should_refuse_children() => Verify(
        "public record Parent(string Id, [ChildrenFrom<Changed>(key: \"X\", identifiedBy: \"Id\")] IEnumerable<State> Children); [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Parent parent) => new(); }",
        Warning("ARCCHR0024").WithArguments("joins or children"));

    [Fact]
    public Task should_refuse_a_fluent_event_property_key() => Verify(
        "[EventType] public record Other(string Id); public record Selected(string Id); public class Projection : IProjectionFor<Selected> { public void Define(IProjectionBuilderFor<Selected> builder) => builder.From<Other>(from => from.UsingKey(e => e.Id)); } [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Selected selected) => new(); }",
        Warning("ARCCHR0024").WithArguments("event property"));

    [Fact]
    public Task should_refuse_an_unsupported_key() => Verify(
        "[FromEvent<Changed>] public record Selected(int Id); [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Selected selected) => new(); }",
        Warning("ARCCHR0024").WithArguments("unsupported key", "string or Guid"));

    [Fact]
    public Task should_admit_an_unfiltered_dependency_free_reducer() => Verify(Reducer +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Balance balance) => new(); }");

    [Fact]
    public Task should_refuse_a_filtered_reducer() => Verify(Reducer.Replace("public class Fold", "[FilterEventsByTag(\"tag\")] public class Fold", StringComparison.Ordinal) +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Balance balance) => new(); }",
        Warning("ARCCHR0024").WithArguments("filters its events"));

    [Fact]
    public Task should_refuse_a_reducer_with_constructor_dependencies() => Verify(Reducer.Replace("public class Fold", "public class Clock; public class Fold(Clock clock)", StringComparison.Ordinal) +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Balance balance) => new(); }",
        Warning("ARCCHR0024").WithArguments("constructor dependencies"));

    [Fact]
    public Task should_admit_a_model_bound_projection_and_a_validator_read() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(State state) => new(); } public class Validator(State state) : CommandValidator<Change>;");

    [Fact]
    public Task should_check_validator_constructor_admission() => Verify(
        "[FromEvent<Changed>(key: \"Owner\")] public record Selected(string Id); [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle() => new(); } public class Validator(Selected selected) : CommandValidator<Change>;",
        Warning("ARCCHR0024").WithArguments("selected", "event property"));

    [Fact]
    public Task should_warn_about_an_aggregate_and_plain_read_on_the_same_key() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Changed Handle(Account account, State state) => new(); }",
        Warning("ARCCHR0033").WithArguments("account", "state", "Owner"));

    [Fact]
    public Task should_allow_an_aggregate_and_a_read_on_a_different_key() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner, MemberId Member) { public Changed Handle(Account account, MemberState state) => new(); }");

    [Theory]
    [InlineData("")]
    [InlineData(", ReadProtection(ReadProtectionMode.Measure)")]
    [InlineData(", ReadProtection(ReadProtectionMode.Off)")]
    public Task should_not_warn_about_aggregate_composition_outside_on(string declaration) => Verify(
        $"public class Account : AggregateRoot; [Command{declaration}] public record Change(OwnerId Owner) {{ public Changed Handle(Account account, State state) => new(); }}");

    [Fact]
    public Task should_explain_that_method_advisory_does_not_cover_parameters() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { [Unprotected(\"body\")] public Changed Handle(State state) => new(); }",
        AnalyzerVerifier<ReadProtectionParameterAnalyzer>.Diagnostic("ARCCHR0034").WithSeverity(DiagnosticSeverity.Info).WithArguments("before its body"));

    [Fact]
    public Task should_not_repeat_method_advice_for_advisory_parameters() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { [Unprotected(\"body\")] public Changed Handle([Unprotected(\"context\")] State state) => new(); }");

    [Theory]
    [InlineData("Measure")]
    [InlineData("On")]
    public Task should_report_exact_scope_coverage_not_proven(string mode) => Verify(
        $"[Command, ReadProtection(ReadProtectionMode.{mode})] public record Change(OwnerId Owner) {{ public EventsWithConcurrencyScopes Handle(State state) => new([], []); }}",
        Warning("ARCCHR0036").WithArguments("coverage", "not proven", "Owner"));

    [Theory]
    [InlineData("")]
    [InlineData(", ReadProtection(ReadProtectionMode.Off)")]
    public Task should_not_report_exact_scope_or_method_advisory_rules_under_off(string declaration) => Verify(
        $"[Command{declaration}] public record Change(OwnerId Owner) {{ [Unprotected(\"body\")] public EventsWithConcurrencyScopes Handle(State state) => new([], []); }}");

    [Fact]
    public Task should_not_report_exact_scopes_for_an_advisory_or_different_source_read() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner, MemberId Member) { public EventsWithConcurrencyScopes Handle([Unprotected(\"context\")] State context, MemberState state) => new([], []); }");

    [Fact]
    public Task should_not_infer_provided_parameter_protection() => Verify(
        "public class Account : AggregateRoot; [Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public State Provide() => new(Owner); public Changed Handle(Account account, State state) => new(); }");

    static DiagnosticBuilder Warning(string id) => AnalyzerVerifier<ReadProtectionParameterAnalyzer>.Diagnostic(id).WithSeverity(DiagnosticSeverity.Warning);
    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) =>
        AnalyzerVerifier<ReadProtectionParameterAnalyzer>.VerifyAnalyzerAsync(Imports + DecisionAnalyzerSource.Definitions + source, diagnostics);
}
