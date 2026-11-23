// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ResponseTypeTraversal;

public class when_inspecting_branching_cycles : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task should_find_optional_events_after_recursive_arms(bool tuples) => Verify<NullableCommandEventReturnAnalyzer>(
        EventCommand("E?", "Outcome", tuples, "Factory()"), Warning());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task should_find_events_in_a_visibly_empty_recursive_union(bool tuples) => Verify<NullableCommandEventReturnAnalyzer>(
        EventCommand("E", "Outcome", tuples, "default"), Warning());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task should_not_invent_events_for_recursive_non_event_unions(bool tuples) => Verify<NullableCommandEventReturnAnalyzer>(
        EventCommand("int", "Outcome", tuples, "default"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task should_find_bare_events_beside_a_guid_through_recursive_tuples(bool tuples) => Verify<CommandEventSourceIdAnalyzer>(
        EventCommand("E", "(Guid, Outcome)", tuples, "default"),
        new ExpectedDiagnostic("ARCCHR0010", DiagnosticSeverity.Warning, "C", "E"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task should_not_route_explicitly_targeted_recursive_events_via_the_guid(bool tuples) => Verify<CommandEventSourceIdAnalyzer>(
        EventCommand("EventForEventSourceId", "(Guid, Outcome)", tuples, "default"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task should_conservatively_require_identity_for_recursive_alternatives(bool tuples) => Verify<CommandEventSourceIdAnalyzer>(
        Imports + BranchingUnionSource.Declarations("EventForEventSourceId", tuples) +
        "[Command] public record C(EventSourceId First, EventSourceId Second) { public Outcome Handle() => default; }",
        new ExpectedDiagnostic("ARCCHR0002", DiagnosticSeverity.Warning, "First", "Second"));

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public Task should_classify_decision_reads_without_reexpanding_active_unions(bool tuples, bool events) => Verify<CommandDecisionReadAnalyzer>(
        DecisionCommand(events ? "Changed" : "int", tuples),
        events ? [new ExpectedDiagnostic("ARCCHR0011", DiagnosticSeverity.Info, "Change", "State")] : []);

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public Task should_classify_keyless_state_reads_without_reexpanding_active_unions(bool tuples, bool events) => Verify<KeylessCommandReadAnalyzer>(
        DecisionCommand(events ? "Changed" : "int", tuples),
        events ? [new ExpectedDiagnostic("ARCCHR0025", DiagnosticSeverity.Warning, "Change", "State")] : []);

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public Task should_classify_exact_scopes_for_concurrency_attributes(bool tuples, bool scopes) => Verify<CommandConcurrencyAttributeAnalyzer>(
        Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer.given.concurrency_source.Command(
            "[EventStreamId(\"id\", concurrency: true)]", "Outcome") +
        BranchingUnionSource.Declarations(scopes ? "EventsWithConcurrencyScopes" : "int", tuples),
        scopes ? [Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer.given.concurrency_source.Warning("EventStreamId")] : []);

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public Task should_classify_exact_scopes_for_protected_reads(bool tuples, bool scopes) => Verify<ReadProtectionParameterAnalyzer>(
        "using Cratis.Chronicle.EventSequences; using Cratis.Arc.Commands; " + DecisionAnalyzerSource.Definitions +
        BranchingUnionSource.Declarations(scopes ? "EventsWithConcurrencyScopes" : "int", tuples) +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public Outcome Handle(State state) => default; }",
        scopes ? [new ExpectedDiagnostic("ARCCHR0036", DiagnosticSeverity.Warning, "coverage", "not proven", "Owner")] : []);

    [Fact]
    public Task should_preserve_optional_container_context_across_a_recursive_case() => Verify<NullableCommandEventReturnAnalyzer>(
        Imports + "[EventType] public record E; public union Outcome(Outcome?, E); " +
        "[Command] public record C { public Outcome Handle() => new E(); }",
        Warning());

    [Fact]
    public Task should_preserve_a_guid_target_inherited_through_a_recursive_tuple_case() => Verify<CommandEventSourceIdAnalyzer>(
        Imports + "[EventType] public record E; public union Outcome((Guid, Outcome), E); " +
        "[Command] public record C { public Outcome Handle() => new E(); }",
        new ExpectedDiagnostic("ARCCHR0010", DiagnosticSeverity.Warning, "C", "E"));

    static string EventCommand(string leaf, string signature, bool tuples, string expression) =>
        Imports + "[EventType] public record E;" + BranchingUnionSource.Declarations(leaf, tuples) +
        "[Command] public record C { public " + signature + " Handle() => " + expression + "; static Outcome Factory() => default; }";

    static string DecisionCommand(string leaf, bool tuples) => DecisionAnalyzerSource.Definitions +
        BranchingUnionSource.Declarations(leaf, tuples) + "[Command] public record Change { public Outcome Handle(State state) => default; }";
}
#endif
