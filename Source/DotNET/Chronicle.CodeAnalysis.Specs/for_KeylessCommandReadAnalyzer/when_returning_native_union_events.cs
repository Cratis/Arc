// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_KeylessCommandReadAnalyzer;

public class when_returning_native_union_events : Specification
{
    [Theory]
    [InlineData("Single")]
    [InlineData("Outcome")]
    [InlineData("ValueTask<Outcome>")]
    [InlineData("(int, Outcome)")]
    public Task should_classify_event_cases_including_a_single_case_union(string signature) => Verify<KeylessCommandReadAnalyzer>(
        DecisionAnalyzerSource.Definitions + "public union Single(Changed); public union Outcome(Changed, string); " +
        "[Command] public record Change { public " + signature + " Handle(State state) => default; }",
        new ExpectedDiagnostic("ARCCHR0025", DiagnosticSeverity.Warning, "Change", "State"));
}
#endif
