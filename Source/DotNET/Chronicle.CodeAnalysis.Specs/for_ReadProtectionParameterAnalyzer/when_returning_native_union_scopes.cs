// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionParameterAnalyzer;

public class when_returning_native_union_scopes : Specification
{
    [Theory]
    [InlineData("Single")]
    [InlineData("Scopes")]
    [InlineData("Task<Scopes>")]
    [InlineData("ValueTask<Scopes>")]
    [InlineData("(int, (string, Scopes))")]
    public Task should_preserve_exact_scope_advice(string signature) => Verify<ReadProtectionParameterAnalyzer>(
        "using Cratis.Chronicle.EventSequences; using Cratis.Arc.Commands; " + DecisionAnalyzerSource.Definitions +
        "public union Single(EventsWithConcurrencyScopes); public union Scopes(EventsWithConcurrencyScopes, string); " +
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner) { public " + signature + " Handle(State state) => default; }",
        new ExpectedDiagnostic("ARCCHR0036", DiagnosticSeverity.Warning, "coverage", "not proven", "Owner"));
}
#endif
