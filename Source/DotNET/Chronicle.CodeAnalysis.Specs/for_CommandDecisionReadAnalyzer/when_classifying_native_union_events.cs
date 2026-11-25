// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_classifying_native_union_events : Specification
{
    [Theory]
    [InlineData("Outcome")]
    [InlineData("Single")]
    [InlineData("Task<Outcome>")]
    [InlineData("ValueTask<Outcome>")]
    [InlineData("(int, Outcome)")]
    [InlineData("OneOf.OneOf<int, Outcome>")]
    public Task should_advise_for_event_producing_cases(string signature) => Verify<CommandDecisionReadAnalyzer>(
        Source(signature), new ExpectedDiagnostic("ARCCHR0011", DiagnosticSeverity.Info, "Change", "State"));

    [Fact]
    public Task should_ignore_non_event_unions() => Verify<CommandDecisionReadAnalyzer>(Source("NotEvents"));

    static string Source(string signature) => DecisionAnalyzerSource.Definitions + "\n" +
        "public union Outcome(Changed, string); public union Single(Changed); public union NotEvents(int, string); " +
        "[Command] public record Change { public " + signature + " Handle(State state) => default; }";
}
#endif
