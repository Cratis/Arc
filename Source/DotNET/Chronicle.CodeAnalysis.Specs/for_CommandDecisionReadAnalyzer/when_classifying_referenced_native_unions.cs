// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_classifying_referenced_native_unions : Specification
{
    [Fact]
    public async Task should_discover_events_in_generated_metadata_constructors()
    {
        var reference = await Emit("using Cratis.Chronicle.Events; [EventType] public record Happened; public union Outcome(Happened, string);");
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(
            DecisionAnalyzerSource.Definitions + "[Command] public record Change { public Outcome Handle(State state) => new Happened(); }",
            [reference],
            new ExpectedDiagnostic("ARCCHR0011", DiagnosticSeverity.Info, "Change", "State"));
    }

    [Fact]
    public async Task should_preserve_nullable_cases_from_metadata()
    {
        var reference = await Emit("using Cratis.Chronicle.Events; [EventType] public record Happened; public union Outcome(Happened?, string);");
        await AnalyzerVerifier<NullableCommandEventReturnAnalyzer>.VerifyAnalyzerAsync(
            Imports + "[Command] public record C { public Outcome Handle() => new Happened(); }",
            [reference],
            Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source.Warning("C", "Happened"));
    }
}
#endif
