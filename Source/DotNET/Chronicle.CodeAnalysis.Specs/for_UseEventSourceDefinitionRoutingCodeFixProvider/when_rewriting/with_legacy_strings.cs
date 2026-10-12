// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseEventSourceDefinitionRoutingCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceDefinitionRoutingCodeFixProvider.when_rewriting;

public class with_legacy_strings : for_WrapperEventRoutingAnalyzer.given.a_wrapper_initializer
{
    [Fact]
    async Task should_keep_unrelated_wrapper_values() => await VerifyCS.VerifyCodeFixAsync(
        Source(@"{|#0:{
        EventSourceType = ""Account"",
        EventStreamType = ""Adjustments"",
        EventStreamId = ""free-form"",
        Tags = [""imported""]
    }|}"),
        Source(@"
    {
        EventSource = typeof(global::AccountEventSource),
        EventStream = ""Adjustments"",
        EventStreamId = ""free-form"",
        Tags = [""imported""]
    }"),
        new ExpectedDiagnostic("ARCCHR0021", DiagnosticSeverity.Info, "Account.Adjustments"));
}
