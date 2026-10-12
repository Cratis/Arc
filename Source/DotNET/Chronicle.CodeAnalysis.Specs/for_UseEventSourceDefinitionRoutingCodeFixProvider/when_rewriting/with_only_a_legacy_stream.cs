// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseEventSourceDefinitionRoutingCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceDefinitionRoutingCodeFixProvider.when_rewriting;

public class with_only_a_legacy_stream : for_WrapperEventRoutingAnalyzer.given.a_wrapper_initializer
{
    [Fact]
    async Task should_supply_the_command_definition() => await VerifyCS.VerifyCodeFixAsync(
        Source("""{|#0:{ EventStreamType = "Transactions" }|}"""),
        Source("""{ EventStream = "Transactions", EventSource = typeof(global::AccountEventSource) }"""),
        new ExpectedDiagnostic("ARCCHR0021", DiagnosticSeverity.Info, "redundant", "drops the definition"));
}
